# TASK-0105: Cấu hình Skill (instructions/answer policy) qua progressive disclosure

## Mục Tiêu
Cung cấp tính năng cấu hình `Skill` để Admin/HR có thể điều chỉnh khả năng trả lời, phạm vi kiến thức (topic, tag), Answer Policy (như required citations), và Prompt Instructions của Agent theo từng use case (Skill) mà không cần deploy lại.

## Kiến Trúc và Thiết Kế

### 1. Skill Entity và Database
- `AskHR.Orchestrator.Models.Agents.Skill`: Lưu trữ siêu dữ liệu về Skill, bao gồm `SkillId`, `Name`, `Instructions`, `PrimaryProvider`, `PrimaryModel`, `Scope`, `AnswerPolicy`.
- Map với MongoDB hoặc Entity Framework tuỳ Provider. (Đang dùng Entity Framework `AgentDbContext`).

### 2. Progressive Disclosure (RAG Orchestration)
Khi nhận được câu hỏi từ User, thay vì nhồi nhét tất cả các policy của HR vào một System Prompt khổng lồ, hệ thống thực hiện:
- Phân loại / Score các Skill khả dụng (`PolicyAnswerService.ResolveSkillAsync`).
- Dựa vào Topic/Tags và keyword matching, hệ thống chọn ra Skill phù hợp nhất.
- Lấy `Instructions` của Skill được chọn và nhúng vào System Prompt ngay trước khi gọi LLM (Progressive Disclosure).
- Nhờ vậy, tiết kiệm Input Tokens và giúp LLM focus vào chính xác nghiệp vụ được phân công.

### 3. Answer Policy
Mỗi Skill có kèm theo `SkillAnswerPolicy` (RequireCitation, RefuseIfExpired). Policy này quyết định cách thức `PolicyAnswerService` từ chối trả lời nếu tài liệu không khớp hoặc không có grounding data.

## API Endpoint
- **SkillsController**: `GET /api/skills`, `POST /api/skills`, `PUT /api/skills/{id}`, `DELETE /api/skills/{id}`.

## Blazor UI
- **SkillManagement.razor**: Trang quản trị chính hiển thị danh sách Skill, trạng thái Approved.
- **SkillConfigEditor.razor**: Component đi kèm cho phép chỉnh sửa cấu trúc JSON-like của Scope và AnswerPolicy.

## Verification
- Unit Tests: `SkillsControllerTests` đảm bảo logic CRUD hoạt động.
- Integration Test: `SkillAndProviderIntegrationTests` đảm bảo khi đổi `Instructions` thì `PolicyAnswerService` nhặt đúng giá trị mới (sau khi Memory Cache hết hạn).
