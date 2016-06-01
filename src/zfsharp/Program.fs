// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.

open Types
open Utility

[<EntryPoint>]
let main argv = 
  let story = Story.load "minizork.z3"
  let interpreter1 = Interpreter.make story
  let interpreter2 = Interpreter.step_instruction interpreter1
  let interpreter3 = Interpreter.step_instruction interpreter2
  let text1 = Interpreter.display interpreter1
  let text2 = Interpreter.display interpreter2
  let text3 = Interpreter.display interpreter3
  Printf.printf "%s\n%s\n%s\n" text1 text2 text3  
  0 // return an integer exit code
