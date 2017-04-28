﻿// Copyright (c) 2017 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module LineDelimitedJsonStream.Stream

open System
open Node.Stream
open Fable.Import.JS
open Fable.Core.JsInterop
open System.Text.RegularExpressions

let private matchNewline x = Regex.Match(x, "\\n")

let private matcher = matchNewline >> function
    | m when m.Success -> Some(m.Index)
    | _ -> None

let private adjustBuff (buff:string) (index:int) =
  let out = buff.Substring(0, index)
  let buff = buff.Substring(index + 1)
  (out, buff)

let private tryJson (onSuccess:obj -> unit) onFail (x:string)  =
  try
    let result = JSON.parse x
    onSuccess(result)
  with
  | ex ->
    let err = !!ex

    onFail err

let rec private getNextMatch (buff:string) (callback:Error option -> obj option -> unit) (turn:int) =
  let opt = matcher(buff)

  match opt with
    | None ->
      if turn = 0 then
        callback None None
      buff
    | Some(index) ->
      let (out, b) = adjustBuff buff index

      tryJson
        (fun x -> callback None (Some x))
        (fun e -> callback (Some e) None )
        out

      getNextMatch b callback (turn + 1)

let getJsonStream () =
  let mutable buff = ""

  let opts = createEmpty<stream_types.TransformBufferOptions>
  opts.readableObjectMode <- Some true
  opts.transform <- Some(fun chunk encoding callback ->
    buff <- getNextMatch
        (buff + chunk.toString("utf-8"))
        callback
        0
  )
  opts.flush <- Some(fun callback ->
    if buff.Length = 0
      then
        callback(None)
      else
        let self = Fable.Core.JsInterop.jsThis

        tryJson
          (fun x ->
            self?push x |> ignore
            callback(None)
          )
          (Some >> callback)
          buff
  )
  stream.Transform.Create opts
