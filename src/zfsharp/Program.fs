// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.

open Types
open Utility

[<EntryPoint>]
let main argv = 
  let story = Story.load "minizork.z3" in
    let instruction = Instruction.decode story (Instruction 0x37d9) in
    let text = Instruction.display instruction (Story.version story) in
    Printf.printf "%s\n" text
  0 // return an integer exit code
