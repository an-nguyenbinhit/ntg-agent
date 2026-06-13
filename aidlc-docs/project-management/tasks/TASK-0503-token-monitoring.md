# TASK-0503: Xem Token Usage & Monitoring

## Mục Tiêu
Cung cấp màn hình hiển thị số lượng token mà ứng dụng đã tiêu thụ, phân tách theo Provider, Model, và User/Session. Giúp HR/Admin kiểm soát chi phí LLM hiệu quả.

## Kiến Trúc và Thiết Kế

### 1. Tracking
- Dữ liệu Token Usage được gửi về `TokenTrackingService` dưới dạng Fire-and-Forget sau mỗi cuộc gọi LLM ở `ModelGateway`.
- Hỗ trợ bóc tách Token Usage thông qua Event Stream (`AgentResponseEvent`) cho các tác vụ sử dụng Agent Workflow.
- Lưu trữ vào Entity `TokenUsage` (có các trường: `InputTokens`, `OutputTokens`, `TotalCost`, `UserId`, `SessionId`, `AgentId`, `ProviderName`, `ModelName`).

### 2. API Aggregation
- `TokenUsageController` cung cấp các endpoint thống kê và lịch sử:
  - `GET /api/TokenUsage/stats`: Trả về tổng token, chi phí, chia theo model, provider. Hỗ trợ filter theo ngày tháng, User.
  - `GET /api/TokenUsage/history`: Trả về danh sách chi tiết các lịch sử request kèm chi phí, có phân trang.
  - `GET /api/TokenUsage/stats/users`: Trả về top N users (hoặc Anonymous sessions) tốn nhiều token nhất.

### 3. Blazor UI
- Component `TokenUsageManagement.razor`: 
  - Giao diện Admin hiển thị Dashboard dạng biểu đồ/số liệu (KPI cards).
  - DataGrid hiển thị History.
  - Hỗ trợ bộ lọc filter thời gian.

## Verification
- Unit Tests: `TokenUsageControllerTests` đã bao phủ toàn bộ các use cases (filter the Date, UserId, Paging limits, Top N).
- Verification thành công với UI tích hợp.
