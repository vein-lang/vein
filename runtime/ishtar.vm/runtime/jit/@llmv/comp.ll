%stack_union = type { [16 x i8], i32 }
%stackval = type { %stack_union, i32 }

define %stackval @comparer(%stackval %first, %stackval %second, i32 %operation) {
entry:
    %_false = alloca i32
    store i32 0, i32* %_false
    %_true = alloca i32
    store i32 1, i32* %_true
    %result = alloca %stackval
    %result_data_ptr = getelementptr %stackval, %stackval* %result, i32 0, i32 0
    %int_ptr = getelementptr %stack_union, %stack_union* %result_data_ptr, i32 0, i32 1
    %result_type_ptr = getelementptr %stackval, %stackval* %result, i32 0, i32 1
    %first_type = extractvalue %stackval %first, 1
    %second_type = extractvalue %stackval %second, 1
    %type_compare = icmp eq i32 %first_type, %second_type
    br i1 %type_compare, label %compare_operation, label %type_mismatch

type_mismatch:
    %false_val_mismatch = load i32, i32* %_false
    store i32 %false_val_mismatch, i32* %int_ptr
    store i32 3, i32* %result_type_ptr  ; TYPE_BOOLEAN
    %result_val = load %stackval, %stackval* %result
    ret %stackval %result_val

compare_operation:
    switch i32 %first_type, label %default [
        i32 3, label %handle_i4  ; TYPE_BOOLEAN
        i32 4, label %handle_i4  ; TYPE_CHAR
        i32 5, label %handle_i1  ; TYPE_I1
        i32 6, label %handle_i2  ; TYPE_I2
        i32 7, label %handle_i4  ; TYPE_I4
    ]

default:
    br label %type_mismatch

handle_i4:
    %first_data_i4 = extractvalue %stackval %first, 0
    %first_i_i4_ptr = getelementptr %stack_union, %stack_union* %first_data_i4, i32 0, i32 1
    %first_i_i4 = load i32, i32* %first_i_i4_ptr
    %second_data_i4 = extractvalue %stackval %second, 0
    %second_i_i4_ptr = getelementptr %stack_union, %stack_union* %second_data_i4, i32 0, i32 1
    %second_i_i4 = load i32, i32* %second_i_i4_ptr
    call void @compare_int(i32 %first_i_i4, i32 %second_i_i4, i32 %operation, %stackval* %result, i32* %_false, i32* %_true)
    %result_val_i4 = load %stackval, %stackval* %result
    ret %stackval %result_val_i4

handle_i1:
    %first_data_i1 = extractvalue %stackval %first, 0
    %first_data_i1_array_ptr = getelementptr %stack_union, %stack_union* %first_data_i1, i32 0, i32 0
    %first_raw_i1_ptr = getelementptr [16 x i8], [16 x i8]* %first_data_i1_array_ptr, i32 0, i32 0
    %first_raw_i1 = load i8, i8* %first_raw_i1_ptr
    %first_i_i1 = sext i8 %first_raw_i1 to i32
    %second_data_i1 = extractvalue %stackval %second, 0
    %second_data_i1_array_ptr = getelementptr %stack_union, %stack_union* %second_data_i1, i32 0, i32 0
    %second_raw_i1_ptr = getelementptr [16 x i8], [16 x i8]* %second_data_i1_array_ptr, i32 0, i32 0
    %second_raw_i1 = load i8, i8* %second_raw_i1_ptr
    %second_i_i1 = sext i8 %second_raw_i1 to i32
    call void @compare_int(i32 %first_i_i1, i32 %second_i_i1, i32 %operation, %stackval* %result, i32* %_false, i32* %_true)
    %result_val_i1 = load %stackval, %stackval* %result
    ret %stackval %result_val_i1

handle_i2:
    %first_data_i2 = extractvalue %stackval %first, 0
    %first_data_i2_array_ptr = getelementptr %stack_union, %stack_union* %first_data_i2, i32 0, i32 0
    %first_raw_i2_ptr = getelementptr [16 x i8], [16 x i8]* %first_data_i2_array_ptr, i32 0, i32 2
    %first_raw_i2 = load i16, i16* %first_raw_i2_ptr
    %first_i_i2 = sext i16 %first_raw_i2 to i32
    %second_data_i2 = extractvalue %stackval %second, 0
    %second_data_i2_array_ptr = getelementptr %stack_union, %stack_union* %second_data_i2, i32 0, i32 0
    %second_raw_i2_ptr = getelementptr [16 x i8], [16 x i8]* %second_data_i2_array_ptr, i32 0, i32 2
    %second_raw_i2 = load i16, i16* %second_raw_i2_ptr
    %second_i_i2 = sext i16 %second_raw_i2 to i32
    call void @compare_int(i32 %first_i_i2, i32 %second_i_i2, i32 %operation, %stackval* %result, i32* %_false, i32* %_true)
    %result_val_i2 = load %stackval, %stackval* %result
    ret %stackval %result_val_i2


declare void @compare_int(i32, i32, i32, %stackval*, i32*, i32*)
declare void @compare_float(float, float, i32, %stackval*, i32*, i32*)

define void @compare_int(i32 %first, i32 %second, i32 %operation, %stackval* %result, i32* %_false, i32* %_true) {
entry:
    %result_data_ptr = getelementptr %stackval, %stackval* %result, i32 0, i32 0
    %int_ptr = getelementptr %stack_union, %stack_union* %result_data_ptr, i32 0, i32 1
    %result_type_ptr = getelementptr %stackval, %stackval* %result, i32 0, i32 1

    switch i32 %operation, label %default_operation [
        i32 88, label %case_lq  ; OpCodeValue.EQL_LQ
        i32 89, label %case_l   ; OpCodeValue.EQL_L
        i32 90, label %case_hq  ; OpCodeValue.EQL_HQ
        i32 91, label %case_h   ; OpCodeValue.EQL_H
        i32 92, label %case_nq  ; OpCodeValue.EQL_NQ
        i32 93, label %case_nn  ; OpCodeValue.EQL_NN
        i32 94, label %case_f   ; OpCodeValue.EQL_F
        i32 95, label %case_t   ; OpCodeValue.EQL_T
    ]

case_f:
    %cmp_f = icmp eq i32 %first, 0
    br i1 %cmp_f, label %return_true, label %return_false

case_h:
    %cmp_h = icmp sgt i32 %first, %second
    br i1 %cmp_h, label %return_true, label %return_false

case_l:
    %cmp_l = icmp slt i32 %first, %second
    br i1 %cmp_l, label %return_true, label %return_false

case_t:
    %cmp_t = icmp eq i32 %first, 1
    br i1 %cmp_t, label %return_true, label %return_false

case_nn:
    %cmp_nn = icmp ne i32 %first, %second
    br i1 %cmp_nn, label %return_true, label %return_false

case_hq:
    %cmp_hq = icmp sge i32 %first, %second
    br i1 %cmp_hq, label %return_true, label %return_false

case_lq:
    %cmp_lq = icmp sle i32 %first, %second
    br i1 %cmp_lq, label %return_true, label %return_false

case_nq:
    %cmp_nq = icmp eq i32 %first, %second
    br i1 %cmp_nq, label %return_true, label %return_false

default_operation:
    br label %return_false

return_true:
    %true_val = load i32, i32* %_true
    store i32 %true_val, i32* %int_ptr
    store i32 3, i32* %result_type_ptr
    ret void

return_false:
    %false_val = load i32, i32* %_false
    store i32 %false_val, i32* %int_ptr
    store i32 3, i32* %result_type_ptr
    ret void
}