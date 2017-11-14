// Copyright (c) 2017 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceScannerDaemon.Handlers

open Fable.Core.JsInterop
open Fable.PowerPack

open EventTypes
open IML.JsonDecoders
open ZFSEventTypes


let mutable deviceMap:Map<DevPath, AddEvent> = Map.empty
let mutable zpoolMap:Map<ZeventGuid, ZfsPool> = Map.empty

type DataMaps = {
  BLOCK_DEVICES: Map<DevPath, AddEvent>;
  ZFSPOOLS: Map<ZeventGuid, ZfsPool>;
}

let (|Info|_|) (x:Map<string,Json.Json>) =
  match x with
    | x when hasAction "info" x -> Some()
    | _ -> None

let updateDatasets action x =
  let matchAction pool =
    match action with
      | "create" -> pool.DATASETS.Add (x.DATASET_UID, x)
      | "destroy" -> pool.DATASETS.Remove x.DATASET_UID
      | _ -> failwith "failure matching dataset action"

  match Map.tryFind x.POOL_UID zpoolMap with
    | Some pool ->
      { pool with DATASETS = matchAction pool }
    | None -> failwith ("Pool to update datasets on is missing!")

let dataHandler (``end``:string option -> unit) x =
  x
   |> unwrapObject
   |> function
      | Info ->
        { BLOCK_DEVICES = deviceMap; ZFSPOOLS = zpoolMap }
          |> toJson
          |> Some
          |> ``end``
      | UdevAdd x | UdevChange x ->
        deviceMap <- Map.add x.DEVPATH x deviceMap
        ``end`` None
      | UdevRemove x ->
        deviceMap <- Map.remove x deviceMap
        ``end`` None
      | ZedPool "create" x ->
        zpoolMap <- Map.add x.UID x zpoolMap
        ``end`` None
      | ZedPool "import" x | ZedExport x ->
        let updatedPool =
          match Map.tryFind x.UID zpoolMap with
            | Some pool ->
              { x with DATASETS = pool.DATASETS }
            | None -> x

        zpoolMap <- Map.add x.UID updatedPool zpoolMap
        ``end`` None
      | ZedDestroy x ->
        zpoolMap <- zpoolMap.Remove x.UID
        ``end`` None
      | ZedDataset "create" x ->
        let updatedPool = updateDatasets "create" x

        zpoolMap <- Map.add x.POOL_UID updatedPool zpoolMap
        ``end`` None
      | ZedDataset "destroy" x ->
        let updatedPool = updateDatasets "destroy" x

        zpoolMap <- Map.add x.POOL_UID updatedPool zpoolMap
        ``end`` None
      | ZedGeneric _ ->
        ``end`` None
      | _ ->
        ``end`` None
        raise (System.Exception "Handler got a bad match")
