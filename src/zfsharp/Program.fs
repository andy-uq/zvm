// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.

open Types
open Utility

[<EntryPoint>]
let main argv = 
  let story = Story.load "minizork.z3"
  let text = Reachability.display_reachable_instructions story (Instruction 0x37d9)
  Printf.printf "%s\n" text
  0 // return an integer exit code
