# TASK-0702: Chọn Provider/Model per Skill mà không cần sửa code (Multi-Provider Route)

## Mục Tiêu
Cho phép cấu hình các `ProviderMetadata` (OpenAI, Azure, Anthropic) và mapping chúng vào từng `ModelRoute` theo Feature (Capability). Hơn thế nữa, cho phép từng `Skill` có thể override Provider/Model mặc định của hệ thống mà không cần developer can thiệp sửa code.

## Kiến Trúc và Thiết Kế

### 1. Database Schema
- **ProviderMetadata**: Chứa thông tin về một provider (VD: `AzureOpenAI`), các capability hỗ trợ (Chat, Embeddings), và secret reference (để lấy API key từ Vault/KeyVault).
- **ModelRoute**: Map một capability (VD: `AnswerGeneration`) tới một Provider và Model cụ thể, hỗ trợ thiết lập danh sách `Fallbacks`.

### 2. Định Tuyến (ModelRouter)
- Lớp `ModelRouter` chịu trách nhiệm đọc các ModelRoutes. 
- Khi có một `ModelCompletionRequest` yêu cầu Capability nào đó, `ModelRouter` sẽ tìm Route có `Feature` tương ứng.
- Nếu `ModelCompletionRequest` chứa `ProviderOverride` hoặc `ModelOverride` (do `Skill` truyền vào), `ModelRouter` sẽ bỏ qua default route và trả về đúng Provider/Model override.
- Tính năng Fallback: Nếu Provider bị down, router có khả năng gọi đến Provider kế tiếp trong mảng `Fallbacks` (tính năng mở rộng ở S-0703/S-0704).

### 3. API Endpoint (ProvidersAdminController)
- `GET /api/ProvidersAdmin/metadata`
- `POST /api/ProvidersAdmin/metadata`
- `PUT /api/ProvidersAdmin/metadata/{id}`
- `GET /api/ProvidersAdmin/routes`
- `POST /api/ProvidersAdmin/routes`
- `PUT /api/ProvidersAdmin/routes/{id}`

## Verification
- Unit Tests: `ProvidersAdminControllerTests` đã cover các thao tác quản lý route.
- Integration Test: Đã được kết hợp vào `SkillAndProviderIntegrationTests` (khi Skill thay đổi provider override, ModelGateway nhận được context mới và sử dụng provider đó thay cho provider mặc định).
