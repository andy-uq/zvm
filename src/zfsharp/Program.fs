// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.

open Types
open Utility

[<EntryPoint>]
let main argv = 
  let story = Story.load @"minizork.z3"
  let version_address = Byte_address 0 in
  let story = Story.load "minizork.z3" in
  let version = Story.read_byte story version_address in
  Printf.printf "%d\n" version

  let zstring = Zstring 0xb106 in
    let text = Zstring.read story zstring in
    Printf.printf "%s\n" text;
  0 // return an integer exit code
