// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.

open Types
open Utility

[<EntryPoint>]
let main argv = 
  let rec interpreter_loop interpreter = 
    interpreter_loop (Interpreter.step_instruction interpreter)
  
  let story = Story.load "minizork.z3"
  interpreter_loop (Interpreter.make story)
  0 // return an integer exit code
