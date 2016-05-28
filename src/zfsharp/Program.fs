// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.

open Types
open Utility

[<EntryPoint>]
let main argv = 
   let () = 
    let addr1 = Byte_address 1 in
    let bytes_a = Immutable_bytes.make "Hello world" in
    let bytes_b = Immutable_bytes.write_byte bytes_a addr1 65 in
    let b_a = Immutable_bytes.read_byte bytes_a addr1 in
    let b_b = Immutable_bytes.read_byte bytes_b addr1 in
    Printf.printf "%d %d\n" b_a b_b
   0 // return an integer exit code
