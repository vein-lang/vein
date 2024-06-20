; ModuleID = '_ffi'
source_filename = "_ffi"
target datalayout = "e-m:w-p270:32:32-p271:32:32-p272:64:64-i64:64-f80:128-n8:16:32:64-S128"

declare i32 @kernel32_GetConsoleWindow()

define { [16 x i8], i32 } @GetConsoleWindow(ptr %0, i32 %1) {
entry:
  %2 = call i32 @kernel32_GetConsoleWindow()
  %retStackVal = alloca { [16 x i8], i32 }, align 8
  %retDataPtr = getelementptr inbounds { [16 x i8], i32 }, ptr %retStackVal, i32 0, i32 0
  store i32 %2, ptr %retDataPtr, align 4
  %retTypePtr = getelementptr inbounds { [16 x i8], i32 }, ptr %retStackVal, i32 0, i32 1
  store i32 9, ptr %retTypePtr, align 4
  %retVal = load { [16 x i8], i32 }, ptr %retStackVal, align 4
  ret { [16 x i8], i32 } %retVal
}
