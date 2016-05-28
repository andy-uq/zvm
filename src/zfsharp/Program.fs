// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.

open Types
open Utility

[<EntryPoint>]
let main argv = 
  let story = Story.load @"minizork.z3"
  let tree = Object.display_object_tree story in
    Printf.printf "%s\n" tree
  0 // return an integer exit code
