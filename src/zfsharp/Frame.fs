﻿module Frame
  open Types

  type t =
    {
      stack : Evaluation_stack.t;
      local_store : Local_store.t;
      resume_at : instruction_address;
      store : variable_location option
    }

  let empty =
    {
      stack = Evaluation_stack.empty;
      local_store = Local_store.empty;
      resume_at = Instruction 0;
      store = None
    }

  let make story arguments routine_address resume_at store = 
    let default_store = Local_store.create_default_locals story routine_address
    let local_store = Local_store.write_arguments default_store arguments
    {
      stack = Evaluation_stack.empty;
      local_store = local_store;
      resume_at = resume_at;
      store = store;
    }

  let resume_at frame =
    frame.resume_at

  let store frame =
    frame.store

  let peek_stack frame =
    Evaluation_stack.peek frame.stack

  let pop_stack frame =
    { frame with stack = Evaluation_stack.pop frame.stack }

  let push_stack frame value =
    { frame with stack = Evaluation_stack.push frame.stack value }

  let write_local frame local value =
    { frame with local_store = Local_store.write_local frame.local_store local value }

  let read_local frame local =
    Local_store.read_local frame.local_store local

  let display frame =
    let (Instruction resume_at) = frame.resume_at in
    let locals = Local_store.display frame.local_store in
    let stack = Evaluation_stack.display frame.stack in
    Printf.sprintf "Locals %s\nStack %s\nResume at:%04x\n"
      locals stack resume_at
