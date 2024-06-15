define i32 @comparer_i32(i32 %first, i32 %second, i32 %operation) {
entry:
    %_true = alloca i32
    store i32 1, i32* %_true
    %_false = alloca i32
    store i32 0, i32* %_false
    %_err = alloca i32
    store i32 2147483647, i32* %_err
    %result = alloca i32
    switch i32 %operation, label %default [
        i32 94, label %case_f   ; EQL_F
        i32 91, label %case_h   ; EQL_H
        i32 89, label %case_l   ; EQL_L
        i32 95, label %case_t   ; EQL_T
        i32 93, label %case_nn  ; EQL_NN
        i32 90, label %case_hq  ; EQL_HQ
        i32 88, label %case_lq  ; EQL_LQ
        i32 92, label %case_nq  ; EQL_NQ
    ]
case_f:
    %cmp_f = icmp eq i32 %first, 0
    %res_f = select i1 %cmp_f, i32 1, i32 0
    store i32 %res_f, i32* %result
    br label %return
case_h:
    %cmp_h = icmp sgt i32 %first, %second
    %res_h = select i1 %cmp_h, i32 1, i32 0
    store i32 %res_h, i32* %result
    br label %return
case_l:
    %cmp_l = icmp slt i32 %first, %second
    %res_l = select i1 %cmp_l, i32 1, i32 0
    store i32 %res_l, i32* %result
    br label %return
case_t:
    %cmp_t = icmp eq i32 %first, 1
    %res_t = select i1 %cmp_t, i32 1, i32 0
    store i32 %res_t, i32* %result
    br label %return
case_nn:
    %cmp_nn = icmp ne i32 %first, %second
    %res_nn = select i1 %cmp_nn, i32 1, i32 0
    store i32 %res_nn, i32* %result
    br label %return
case_hq:
    %cmp_hq = icmp sge i32 %first, %second
    %res_hq = select i1 %cmp_hq, i32 1, i32 0
    store i32 %res_hq, i32* %result
    br label %return
case_lq:
    %cmp_lq = icmp sle i32 %first, %second
    %res_lq = select i1 %cmp_lq, i32 1, i32 0
    store i32 %res_lq, i32* %result
    br label %return
case_nq:
    %cmp_nq = icmp eq i32 %first, %second
    %res_nq = select i1 %cmp_nq, i32 1, i32 0
    store i32 %res_nq, i32* %result
    br label %return
default:
    %err_val = load i32, i32* %_err
    store i32 %err_val, i32* %result
    br label %return
return:
    %result_val = load i32, i32* %result
    ret i32 %result_val
}

define i32 @comparer_u8(i8 %first, i8 %second, i32 %operation) {
entry:
  %_true = alloca i32
  store i32 1, i32* %_true
  %_false = alloca i32
  store i32 0, i32* %_false
  %_err = alloca i32
  store i32 2147483647, i32* %_err
  %result = alloca i32

  switch i32 %operation, label %default [
    i32 94, label %case_f
    i32 91, label %case_h
    i32 89, label %case_l
    i32 95, label %case_t
    i32 93, label %case_nn
    i32 90, label %case_hq
    i32 88, label %case_lq
    i32 92, label %case_nq
  ]

case_f:
  %cmp_f = icmp eq i8 %first, 0
  %res_f = select i1 %cmp_f, i32 1, i32 0
  store i32 %res_f, i32* %result
  br label %return

case_h:
  %cmp_h = icmp ugt i8 %first, %second
  %res_h = select i1 %cmp_h, i32 1, i32 0
  store i32 %res_h, i32* %result
  br label %return

case_l:
  %cmp_l = icmp ult i8 %first, %second
  %res_l = select i1 %cmp_l, i32 1, i32 0
  store i32 %res_l, i32* %result
  br label %return

case_t:
  %cmp_t = icmp eq i8 %first, 1
  %res_t = select i1 %cmp_t, i32 1, i32 0
  store i32 %res_t, i32* %result
  br label %return

case_nn:
  %cmp_nn = icmp ne i8 %first, %second
  %res_nn = select i1 %cmp_nn, i32 1, i32 0
  store i32 %res_nn, i32* %result
  br label %return

case_hq:
  %cmp_hq = icmp uge i8 %first, %second
  %res_hq = select i1 %cmp_hq, i32 1, i32 0
  store i32 %res_hq, i32* %result
  br label %return

case_lq:
  %cmp_lq = icmp ule i8 %first, %second
  %res_lq = select i1 %cmp_lq, i32 1, i32 0
  store i32 %res_lq, i32* %result
  br label %return

case_nq:
  %cmp_nq = icmp eq i8 %first, %second
  %res_nq = select i1 %cmp_nq, i32 1, i32 0
  store i32 %res_nq, i32* %result
  br label %return

default:
  %err_val = load i32, i32* %_err
  store i32 %err_val, i32* %result
  br label %return

return:
  %result_val = load i32, i32* %result
  ret i32 %result_val
}

define i32 @comparer_i8(i8 %first, i8 %second, i32 %operation) {
entry:
  %_true = alloca i32
  store i32 1, i32* %_true
  %_false = alloca i32
  store i32 0, i32* %_false
  %_err = alloca i32
  store i32 2147483647, i32* %_err
  %result = alloca i32

  switch i32 %operation, label %default [
    i32 94, label %case_f
    i32 91, label %case_h
    i32 89, label %case_l
    i32 95, label %case_t
    i32 93, label %case_nn
    i32 90, label %case_hq
    i32 88, label %case_lq
    i32 92, label %case_nq
  ]

case_f:
  %cmp_f = icmp eq i8 %first, 0
  %res_f = select i1 %cmp_f, i32 1, i32 0
  store i32 %res_f, i32* %result
  br label %return

case_h:
  %cmp_h = icmp sgt i8 %first, %second
  %res_h = select i1 %cmp_h, i32 1, i32 0
  store i32 %res_h, i32* %result
  br label %return

case_l:
  %cmp_l = icmp slt i8 %first, %second
  %res_l = select i1 %cmp_l, i32 1, i32 0
  store i32 %res_l, i32* %result
  br label %return

case_t:
  %cmp_t = icmp eq i8 %first, 1
  %res_t = select i1 %cmp_t, i32 1, i32 0
  store i32 %res_t, i32* %result
  br label %return

case_nn:
  %cmp_nn = icmp ne i8 %first, %second
  %res_nn = select i1 %cmp_nn, i32 1, i32 0
  store i32 %res_nn, i32* %result
  br label %return

case_hq:
  %cmp_hq = icmp sge i8 %first, %second
  %res_hq = select i1 %cmp_hq, i32 1, i32 0
  store i32 %res_hq, i32* %result
  br label %return

case_lq:
  %cmp_lq = icmp sle i8 %first, %second
  %res_lq = select i1 %cmp_lq, i32 1, i32 0
  store i32 %res_lq, i32* %result
  br label %return

case_nq:
  %cmp_nq = icmp eq i8 %first, %second
  %res_nq = select i1 %cmp_nq, i32 1, i32 0
  store i32 %res_nq, i32* %result
  br label %return

default:
  %err_val = load i32, i32* %_err
  store i32 %err_val, i32* %result
  br label %return

return:
  %result_val = load i32, i32* %result
  ret i32 %result_val
}

define i32 @comparer_i16(i16 %first, i16 %second, i32 %operation) {
entry:
  %_true = alloca i32
  store i32 1, i32* %_true
  %_false = alloca i32
  store i32 0, i32* %_false
  %_err = alloca i32
  store i32 2147483647, i32* %_err
  %result = alloca i32

  switch i32 %operation, label %default [
    i32 94, label %case_f
    i32 91, label %case_h
    i32 89, label %case_l
    i32 95, label %case_t
    i32 93, label %case_nn
    i32 90, label %case_hq
    i32 88, label %case_lq
    i32 92, label %case_nq
  ]

case_f:
  %cmp_f = icmp eq i16 %first, 0
  %res_f = select i1 %cmp_f, i32 1, i32 0
  store i32 %res_f, i32* %result
  br label %return

case_h:
  %cmp_h = icmp sgt i16 %first, %second
  %res_h = select i1 %cmp_h, i32 1, i32 0
  store i32 %res_h, i32* %result
  br label %return

case_l:
  %cmp_l = icmp slt i16 %first, %second
  %res_l = select i1 %cmp_l, i32 1, i32 0
  store i32 %res_l, i32* %result
  br label %return

case_t:
  %cmp_t = icmp eq i16 %first, 1
  %res_t = select i1 %cmp_t, i32 1, i32 0
  store i32 %res_t, i32* %result
  br label %return

case_nn:
  %cmp_nn = icmp ne i16 %first, %second
  %res_nn = select i1 %cmp_nn, i32 1, i32 0
  store i32 %res_nn, i32* %result
  br label %return

case_hq:
  %cmp_hq = icmp sge i16 %first, %second
  %res_hq = select i1 %cmp_hq, i32 1, i32 0
  store i32 %res_hq, i32* %result
  br label %return

case_lq:
  %cmp_lq = icmp sle i16 %first, %second
  %res_lq = select i1 %cmp_lq, i32 1, i32 0
  store i32 %res_lq, i32* %result
  br label %return

case_nq:
  %cmp_nq = icmp eq i16 %first, %second
  %res_nq = select i1 %cmp_nq, i32 1, i32 0
  store i32 %res_nq, i32* %result
  br label %return

default:
  %err_val = load i32, i32* %_err
  store i32 %err_val, i32* %result
  br label %return

return:
  %result_val = load i32, i32* %result
  ret i32 %result_val
}

define i32 @comparer_u16(i16 %first, i16 %second, i32 %operation) {
entry:
  %_true = alloca i32
  store i32 1, i32* %_true
  %_false = alloca i32
  store i32 0, i32* %_false
  %_err = alloca i32
  store i32 2147483647, i32* %_err
  %result = alloca i32

  switch i32 %operation, label %default [
    i32 94, label %case_f
    i32 91, label %case_h
    i32 89, label %case_l
    i32 95, label %case_t
    i32 93, label %case_nn
    i32 90, label %case_hq
    i32 88, label %case_lq
    i32 92, label %case_nq
  ]

case_f:
  %cmp_f = icmp eq i16 %first, 0
  %res_f = select i1 %cmp_f, i32 1, i32 0
  store i32 %res_f, i32* %result
  br label %return

case_h:
  %cmp_h = icmp ugt i16 %first, %second
  %res_h = select i1 %cmp_h, i32 1, i32 0
  store i32 %res_h, i32* %result
  br label %return

case_l:
  %cmp_l = icmp ult i16 %first, %second
  %res_l = select i1 %cmp_l, i32 1, i32 0
  store i32 %res_l, i32* %result
  br label %return

case_t:
  %cmp_t = icmp eq i16 %first, 1
  %res_t = select i1 %cmp_t, i32 1, i32 0
  store i32 %res_t, i32* %result
  br label %return

case_nn:
  %cmp_nn = icmp ne i16 %first, %second
  %res_nn = select i1 %cmp_nn, i32 1, i32 0
  store i32 %res_nn, i32* %result
  br label %return

case_hq:
  %cmp_hq = icmp uge i16 %first, %second
  %res_hq = select i1 %cmp_hq, i32 1, i32 0
  store i32 %res_hq, i32* %result
  br label %return

case_lq:
  %cmp_lq = icmp ule i16 %first, %second
  %res_lq = select i1 %cmp_lq, i32 1, i32 0
  store i32 %res_lq, i32* %result
  br label %return

case_nq:
  %cmp_nq = icmp eq i16 %first, %second
  %res_nq = select i1 %cmp_nq, i32 1, i32 0
  store i32 %res_nq, i32* %result
  br label %return

default:
  %err_val = load i32, i32* %_err
  store i32 %err_val, i32* %result
  br label %return

return:
  %result_val = load i32, i32* %result
  ret i32 %result_val
}

define i32 @comparer_u32(i32 %first, i32 %second, i32 %operation) {
entry:
  %_true = alloca i32
  store i32 1, i32* %_true
  %_false = alloca i32
  store i32 0, i32* %_false
  %_err = alloca i32
  store i32 2147483647, i32* %_err
  %result = alloca i32

  switch i32 %operation, label %default [
    i32 94, label %case_f
    i32 91, label %case_h
    i32 89, label %case_l
    i32 95, label %case_t
    i32 93, label %case_nn
    i32 90, label %case_hq
    i32 88, label %case_lq
    i32 92, label %case_nq
  ]

case_f:
  %cmp_f = icmp eq i32 %first, 0
  %res_f = select i1 %cmp_f, i32 1, i32 0
  store i32 %res_f, i32* %result
  br label %return

case_h:
  %cmp_h = icmp ugt i32 %first, %second
  %res_h = select i1 %cmp_h, i32 1, i32 0
  store i32 %res_h, i32* %result
  br label %return

case_l:
  %cmp_l = icmp ult i32 %first, %second
  %res_l = select i1 %cmp_l, i32 1, i32 0
  store i32 %res_l, i32* %result
  br label %return

case_t:
  %cmp_t = icmp eq i32 %first, 1
  %res_t = select i1 %cmp_t, i32 1, i32 0
  store i32 %res_t, i32* %result
  br label %return

case_nn:
  %cmp_nn = icmp ne i32 %first, %second
  %res_nn = select i1 %cmp_nn, i32 1, i32 0
  store i32 %res_nn, i32* %result
  br label %return

case_hq:
  %cmp_hq = icmp uge i32 %first, %second
  %res_hq = select i1 %cmp_hq, i32 1, i32 0
  store i32 %res_hq, i32* %result
  br label %return

case_lq:
  %cmp_lq = icmp ule i32 %first, %second
  %res_lq = select i1 %cmp_lq, i32 1, i32 0
  store i32 %res_lq, i32* %result
  br label %return

case_nq:
  %cmp_nq = icmp eq i32 %first, %second
  %res_nq = select i1 %cmp_nq, i32 1, i32 0
  store i32 %res_nq, i32* %result
  br label %return

default:
  %err_val = load i32, i32* %_err
  store i32 %err_val, i32* %result
  br label %return

return:
  %result_val = load i32, i32* %result
  ret i32 %result_val
}

define i32 @comparer_u64(i64 %first, i64 %second, i32 %operation) {
entry:
  %_true = alloca i32
  store i32 1, i32* %_true
  %_false = alloca i32
  store i32 0, i32* %_false
  %_err = alloca i32
  store i32 2147483647, i32* %_err
  %result = alloca i32

  switch i32 %operation, label %default [
    i32 94, label %case_f
    i32 91, label %case_h
    i32 89, label %case_l
    i32 95, label %case_t
    i32 93, label %case_nn
    i32 90, label %case_hq
    i32 88, label %case_lq
    i32 92, label %case_nq
  ]

case_f:
  %cmp_f = icmp eq i64 %first, 0
  %res_f = select i1 %cmp_f, i32 1, i32 0
  store i32 %res_f, i32* %result
  br label %return

case_h:
  %cmp_h = icmp ugt i64 %first, %second
  %res_h = select i1 %cmp_h, i32 1, i32 0
  store i32 %res_h, i32* %result
  br label %return

case_l:
  %cmp_l = icmp ult i64 %first, %second
  %res_l = select i1 %cmp_l, i32 1, i32 0
  store i32 %res_l, i32* %result
  br label %return

case_t:
  %cmp_t = icmp eq i64 %first, 1
  %res_t = select i1 %cmp_t, i32 1, i32 0
  store i32 %res_t, i32* %result
  br label %return

case_nn:
  %cmp_nn = icmp ne i64 %first, %second
  %res_nn = select i1 %cmp_nn, i32 1, i32 0
  store i32 %res_nn, i32* %result
  br label %return

case_hq:
  %cmp_hq = icmp uge i64 %first, %second
  %res_hq = select i1 %cmp_hq, i32 1, i32 0
  store i32 %res_hq, i32* %result
  br label %return

case_lq:
  %cmp_lq = icmp ule i64 %first, %second
  %res_lq = select i1 %cmp_lq, i32 1, i32 0
  store i32 %res_lq, i32* %result
  br label %return

case_nq:
  %cmp_nq = icmp eq i64 %first, %second
  %res_nq = select i1 %cmp_nq, i32 1, i32 0
  store i32 %res_nq, i32* %result
  br label %return

default:
  %err_val = load i32, i32* %_err
  store i32 %err_val, i32* %result
  br label %return

return:
  %result_val = load i32, i32* %result
  ret i32 %result_val
}

define i32 @comparer_i64(i64 %first, i64 %second, i32 %operation) {
entry:
  %_true = alloca i32
  store i32 1, i32* %_true
  %_false = alloca i32
  store i32 0, i32* %_false
  %_err = alloca i32
  store i32 2147483647, i32* %_err
  %result = alloca i32

  switch i32 %operation, label %default [
    i32 94, label %case_f
    i32 91, label %case_h
    i32 89, label %case_l
    i32 95, label %case_t
    i32 93, label %case_nn
    i32 90, label %case_hq
    i32 88, label %case_lq
    i32 92, label %case_nq
  ]

case_f:
  %cmp_f = icmp eq i64 %first, 0
  %res_f = select i1 %cmp_f, i32 1, i32 0
  store i32 %res_f, i32* %result
  br label %return

case_h:
  %cmp_h = icmp sgt i64 %first, %second
  %res_h = select i1 %cmp_h, i32 1, i32 0
  store i32 %res_h, i32* %result
  br label %return

case_l:
  %cmp_l = icmp slt i64 %first, %second
  %res_l = select i1 %cmp_l, i32 1, i32 0
  store i32 %res_l, i32* %result
  br label %return

case_t:
  %cmp_t = icmp eq i64 %first, 1
  %res_t = select i1 %cmp_t, i32 1, i32 0
  store i32 %res_t, i32* %result
  br label %return

case_nn:
  %cmp_nn = icmp ne i64 %first, %second
  %res_nn = select i1 %cmp_nn, i32 1, i32 0
  store i32 %res_nn, i32* %result
  br label %return

case_hq:
  %cmp_hq = icmp sge i64 %first, %second
  %res_hq = select i1 %cmp_hq, i32 1, i32 0
  store i32 %res_hq, i32* %result
  br label %return

case_lq:
  %cmp_lq = icmp sle i64 %first, %second
  %res_lq = select i1 %cmp_lq, i32 1, i32 0
  store i32 %res_lq, i32* %result
  br label %return

case_nq:
  %cmp_nq = icmp eq i64 %first, %second
  %res_nq = select i1 %cmp_nq, i32 1, i32 0
  store i32 %res_nq, i32* %result
  br label %return

default:
  %err_val = load i32, i32* %_err
  store i32 %err_val, i32* %result
  br label %return

return:
  %result_val = load i32, i32* %result
  ret i32 %result_val
}

define i32 @comparer_f16(float %first, float %second, i32 %operation) {
entry:
  %_true = alloca i32
  store i32 1, i32* %_true
  %_false = alloca i32
  store i32 0, i32* %_false
  %_err = alloca i32
  store i32 2147483647, i32* %_err
  %result = alloca i32

  switch i32 %operation, label %default [
    i32 94, label %case_f
    i32 91, label %case_h
    i32 89, label %case_l
    i32 95, label %case_t
    i32 93, label %case_nn
    i32 90, label %case_hq
    i32 88, label %case_lq
    i32 92, label %case_nq
  ]

case_f:
  %cmp_f = fcmp oeq float %first, 0.0
  %res_f = select i1 %cmp_f, i32 1, i32 0
  store i32 %res_f, i32* %result
  br label %return

case_h:
  %cmp_h = fcmp ogt float %first, %second
  %res_h = select i1 %cmp_h, i32 1, i32 0
  store i32 %res_h, i32* %result
  br label %return

case_l:
  %cmp_l = fcmp olt float %first, %second
  %res_l = select i1 %cmp_l, i32 1, i32 0
  store i32 %res_l, i32* %result
  br label %return

case_t:
  %cmp_t = fcmp oeq float %first, 1.0
  %res_t = select i1 %cmp_t, i32 1, i32 0
  store i32 %res_t, i32* %result
  br label %return

case_nn:
  %cmp_nn = fcmp one float %first, %second
  %res_nn = select i1 %cmp_nn, i32 1, i32 0
  store i32 %res_nn, i32* %result
  br label %return

case_hq:
  %cmp_hq = fcmp oge float %first, %second
  %res_hq = select i1 %cmp_hq, i32 1, i32 0
  store i32 %res_hq, i32* %result
  br label %return

case_lq:
  %cmp_lq = fcmp ole float %first, %second
  %res_lq = select i1 %cmp_lq, i32 1, i32 0
  store i32 %res_lq, i32* %result
  br label %return

case_nq:
  %cmp_nq = fcmp oeq float %first, %second
  %res_nq = select i1 %cmp_nq, i32 1, i32 0
  store i32 %res_nq, i32* %result
  br label %return

default:
  %err_val = load i32, i32* %_err
  store i32 %err_val, i32* %result
  br label %return

return:
  %result_val = load i32, i32* %result
  ret i32 %result_val
}

define i32 @comparer_f32(double %first, double %second, i32 %operation) {
entry:
  %_true = alloca i32
  store i32 1, i32* %_true
  %_false = alloca i32
  store i32 0, i32* %_false
  %_err = alloca i32
  store i32 2147483647, i32* %_err
  %result = alloca i32

  switch i32 %operation, label %default [
    i32 94, label %case_f
    i32 91, label %case_h
    i32 89, label %case_l
    i32 95, label %case_t
    i32 93, label %case_nn
    i32 90, label %case_hq
    i32 88, label %case_lq
    i32 92, label %case_nq
  ]

case_f:
  %cmp_f = fcmp oeq double %first, 0.0
  %res_f = select i1 %cmp_f, i32 1, i32 0
  store i32 %res_f, i32* %result
  br label %return

case_h:
  %cmp_h = fcmp ogt double %first, %second
  %res_h = select i1 %cmp_h, i32 1, i32 0
  store i32 %res_h, i32* %result
  br label %return

case_l:
  %cmp_l = fcmp olt double %first, %second
  %res_l = select i1 %cmp_l, i32 1, i32 0
  store i32 %res_l, i32* %result
  br label %return

case_t:
  %cmp_t = fcmp oeq double %first, 1.0
  %res_t = select i1 %cmp_t, i32 1, i32 0
  store i32 %res_t, i32* %result
  br label %return

case_nn:
  %cmp_nn = fcmp one double %first, %second
  %res_nn = select i1 %cmp_nn, i32 1, i32 0
  store i32 %res_nn, i32* %result
  br label %return

case_hq:
  %cmp_hq = fcmp oge double %first, %second
  %res_hq = select i1 %cmp_hq, i32 1, i32 0
  store i32 %res_hq, i32* %result
  br label %return

case_lq:
  %cmp_lq = fcmp ole double %first, %second
  %res_lq = select i1 %cmp_lq, i32 1, i32 0
  store i32 %res_lq, i32* %result
  br label %return

case_nq:
  %cmp_nq = fcmp oeq double %first, %second
  %res_nq = select i1 %cmp_nq, i32 1, i32 0
  store i32 %res_nq, i32* %result
  br label %return

default:
  %err_val = load i32, i32* %_err
  store i32 %err_val, i32* %result
  br label %return

return:
  %result_val = load i32, i32* %result
  ret i32 %result_val
}

define i32 @comparer_i128(i128 %first, i128 %second, i32 %operation) {
entry:
  %_true = alloca i32
  store i32 1, i32* %_true
  %_false = alloca i32
  store i32 0, i32* %_false
  %_err = alloca i32
  store i32 2147483647, i32* %_err
  %result = alloca i32

  switch i32 %operation, label %default [
    i32 94, label %case_f
    i32 91, label %case_h
    i32 89, label %case_l
    i32 95, label %case_t
    i32 93, label %case_nn
    i32 90, label %case_hq
    i32 88, label %case_lq
    i32 92, label %case_nq
  ]

case_f:
  %cmp_f = icmp eq i128 %first, 0
  %res_f = select i1 %cmp_f, i32 1, i32 0
  store i32 %res_f, i32* %result
  br label %return

case_h:
  %cmp_h = icmp sgt i128 %first, %second
  %res_h = select i1 %cmp_h, i32 1, i32 0
  store i32 %res_h, i32* %result
  br label %return

case_l:
  %cmp_l = icmp slt i128 %first, %second
  %res_l = select i1 %cmp_l, i32 1, i32 0
  store i32 %res_l, i32* %result
  br label %return

case_t:
  %cmp_t = icmp eq i128 %first, 1
  %res_t = select i1 %cmp_t, i32 1, i32 0
  store i32 %res_t, i32* %result
  br label %return

case_nn:
  %cmp_nn = icmp ne i128 %first, %second
  %res_nn = select i1 %cmp_nn, i32 1, i32 0
  store i32 %res_nn, i32* %result
  br label %return

case_hq:
  %cmp_hq = icmp sge i128 %first, %second
  %res_hq = select i1 %cmp_hq, i32 1, i32 0
  store i32 %res_hq, i32* %result
  br label %return

case_lq:
  %cmp_lq = icmp sle i128 %first, %second
  %res_lq = select i1 %cmp_lq, i32 1, i32 0
  store i32 %res_lq, i32* %result
  br label %return

case_nq:
  %cmp_nq = icmp eq i128 %first, %second
  %res_nq = select i1 %cmp_nq, i32 1, i32 0
  store i32 %res_nq, i32* %result
  br label %return

default:
  %err_val = load i32, i32* %_err
  store i32 %err_val, i32* %result
  br label %return

return:
  %result_val = load i32, i32* %result
  ret i32 %result_val
}