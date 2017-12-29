// Copyright (c) 2017 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceScannerDaemon.Handlers

open Fable.Core.JsInterop

open Fable.Import.Node.PowerPack.LineDelimitedJsonStream
open Fable.Import.Node
open Udev
open Zed

type Data = {
  mutable blockDevices: Map<DevPath, UEvent>;
  mutable zpools: Map<Zpool.Guid, Zpool.Data>;
  mutable zfs: Map<Zfs.Id, Zfs.Data>;
  mutable props: Properties.Property list
}

let data = {
  blockDevices = Map.empty;
  zpools = Map.empty;
  zfs = Map.empty;
  props = [];
}

let (|Info|_|) (x:Json) =
  match actionDecoder x with
    | Ok(y) when y = "info" -> Some()
    | _ -> None

let dataHandler (``end``:Buffer.Buffer option -> unit) (x:Json) =
  match x with
    | Info ->
        data
        |> toJson
        |> Buffer.Buffer.from
        |> Some
        |> ``end``
    | UdevAdd x | UdevChange x ->
      data.blockDevices <- Map.add x.DEVPATH x data.blockDevices
      ``end`` None
    | UdevRemove x ->
      data.blockDevices <- Map.add x.DEVPATH x data.blockDevices
      ``end`` None
    | Zpool.Create x ->
      data.zpools <- Map.add x.guid x data.zpools
      ``end`` None
    | Zpool.Import x | Zpool.Export x ->
      data.zpools <- Map.add x.guid x data.zpools
      ``end`` None
    | Zpool.Destroy x ->
      data.zpools <- Map.remove x.guid data.zpools
      // Remove any datasets and props.
      ``end`` None
    | Zfs.Create x ->
      data.zfs <- Map.add x.id x data.zfs
      ``end`` None
    | Zfs.Destroy x ->
      data.zfs <- Map.remove x.id data.zfs
      ``end`` None
    | Properties.ZpoolProp (x:Properties.Property) ->
      data.props <- (data.props @ [x])
      ``end`` None
    | Properties.ZfsProp x ->
      data.props <- (data.props @ [x])
      ``end`` None
    | ZedGeneric ->
      ``end`` None
    | _ ->
      ``end`` None
      failwith "Handler got a bad match"
