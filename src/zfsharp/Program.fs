// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.

open Types
open Utility

[<EntryPoint>]
let main argv = 
  let story = Story.load "minizork.z3"
  let locals = Local_store.create_default_locals story (Routine 0x3b36) in
  let text = Local_store.display locals in
  Printf.printf "%s\n" text
  0 // return an integer exit code
