 1. Khi bên A gửi data:
Validate request.

Lưu xuống DB một bản ghi Pending (ví dụ bảng ForwardRequestLog).

Trả lại cho A: "Đã nhận yêu cầu, đang xử lý" (202 Accepted hoặc 200 OK).

Dùng background job (Hangfire hoặc Queue) để thực hiện gửi lên C sau đó.

 2. Nếu gửi C thành công:
Update bản ghi trong DB là Success.

Lưu response của C để truy vết.

 3. Nếu gửi C thất bại:
Update RetryCount, LastErrorMessage, NextRetryAt.

Job sẽ tự động retry sau 5-10 phút hoặc theo lịch bạn đặt.