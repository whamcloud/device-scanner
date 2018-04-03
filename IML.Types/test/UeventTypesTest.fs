// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.Types.UeventTypesTest

open UeventTypes

open Fable.Import.Jest
open Fable.Import
open Fable.Core.JsInterop
open IML.CommonLibrary
open Thot.Json
open Matchers
open Fixtures

[
  ("add", fixtures.add);
  ("add DM", fixtures.addDm);
  ("remove", fixtures.remove);
  ("MdRaid", fixtures.addMdRaid);
]
  |> List.map (fun ((name, x)) ->
    Test(name, fun () ->
      expect.assertions 2

      let decoded =
        x
        |> UEvent.udevDecoder
        |> Result.unwrap

      toMatchSnapshot decoded

      Map.ofList [
          (decoded.devpath, decoded);
        ]
        |> BlockDevices.encoder
        |> Thot.Json.Encode.encode 2
        |> toMatchSnapshot
    )
  )
  |> testList "decode / encode UEvents "

// Reuse our snapshots to test decoding
let snaps:JS.Object = importAll "./__snapshots__/UeventTypesTest.fs.snap"

let ks =
  JS.Object.keys snaps
    |> Seq.filter (String.endsWith "2")
    |> Seq.map (fun k ->
      let entry:string =
        !!snaps?(k)
          |> String.filter (fun x -> x <> '\n')

      Test(k, fun () ->
        !!JS.JSON.parse(entry)
          |> Decode.decodeString (BlockDevices.decoder)
          |> Result.unwrap
          |> BlockDevices.encoder
          |> Thot.Json.Encode.encode 2
          |> toMatchSnapshot
      )
    )
    |> testList "decode encoded"
