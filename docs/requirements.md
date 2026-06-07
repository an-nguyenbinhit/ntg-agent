# Tài liệu Yêu cầu — NTG Agent (Enterprise RAG Chatbot)

> Tài liệu mô tả yêu cầu chức năng cho nền tảng chatbot doanh nghiệp dựa trên RAG.
> Mỗi yêu cầu được gắn trạng thái triển khai hiện tại để theo dõi tiến độ và khoảng trống (gap).

**Chú thích trạng thái:**

| Ký hiệu | Ý nghĩa |
|---|---|
| ✅ Đã có | Đã triển khai trong codebase |
| ⚠️ Một phần | Có nền tảng nhưng chưa hoàn chỉnh |
| ❌ Chưa có | Cần triển khai mới |
| 🚫 Ngoài phạm vi | Không nằm trong phạm vi dự án |

---

## 1. Module Quản lý và Xử lý tài liệu (Document Ingestion & Processing)

Biến tài liệu thô thành kiến thức mà AI có thể truy vấn.

| ID | Yêu cầu | Trạng thái | Ghi chú triển khai |
|---|---|---|---|
| FR-1.1 | Tải lên tài liệu chính sách, quy định, hợp đồng (đa định dạng) | ✅ Đã có | `DocumentsController`, dịch vụ `NTG.Agent.Knowledge` |
| FR-1.2 | Phân tích cấu trúc tài liệu (tiêu đề, đoạn văn, bảng biểu) | ✅ Đã có | `DocumentAnalysisService` (Azure Document Intelligence: OCR, bảng, selection marks) + pipeline Kernel Memory |
| FR-1.3 | Phân mảnh ngữ nghĩa (Chunking) tài liệu dài | ✅ Đã có | Pipeline handlers của Kernel Memory |
| FR-1.4 | Mã hóa văn bản thành vector và lưu vào kho kiến thức | ✅ Đã có | Embedding + vector store qua Kernel Memory |
| FR-1.5 | Tổ chức tài liệu theo thư mục phân cấp | ✅ Đã có | Entity `Folder` |

---

## 2. Module Định danh Người dùng (Identity Management)

> **Lưu ý phạm vi:** Multi-tenancy (cách ly dữ liệu đa tổ chức/người thuê) **🚫 Ngoài phạm vi** dự án này. Cách ly dữ liệu hiện được thực hiện theo `AgentId` + `Tag/Role` thay vì theo tenant.

| ID | Yêu cầu | Trạng thái | Ghi chú triển khai |
|---|---|---|---|
| FR-2.1 | Xác thực và định danh người dùng đăng nhập | ✅ Đã có | ASP.NET Identity (`User`/`Role`/`UserRole`), cookie auth dùng chung |
| FR-2.2 | Hỗ trợ phiên ẩn danh (anonymous) có giới hạn | ✅ Đã có | `AnonymousSession`, rate-limit theo SessionId/IP |
| FR-2.3 | Cách ly dữ liệu theo Agent | ✅ Đã có | Filter `agentId` khi import & search trong Kernel Memory |
| FR-2.4 | Cách ly dữ liệu đa tổ chức (multi-tenant) | 🚫 Ngoài phạm vi | — |

---

## 3. Module Kiểm soát Truy cập & Bảo mật (Security Trimming & Authorization)

| ID | Yêu cầu | Trạng thái | Ghi chú triển khai |
|---|---|---|---|
| FR-3.1 | Gắn nhãn bảo mật cho từng tài liệu/đoạn khi lưu trữ | ✅ Đã có | `Tag` / `DocumentTag` / `TagRole`; gắn tag khi import (`KernelMemoryKnowledge.ComposeTags`) |
| FR-3.2 | Lọc dữ liệu động trước khi đưa cho AI (Security Trimming) | ✅ Đã có | `AgentService.GetUserTags()` → `ComposeFilters()` đẩy filter vào Kernel Memory **trước** khi search |
| FR-3.3 | Phân quyền theo vai trò (Role-based) | ✅ Đã có | Quyền truy cập tag ánh xạ qua `TagRole` |
| FR-3.4 | Áp dụng nhãn bảo mật mặc định cho user ẩn danh | ✅ Đã có | `AnonymousRoleId` |

---

## 4. Module Điều phối Tìm kiếm và Trả lời (Conversational Query Orchestration)

| ID | Yêu cầu | Trạng thái | Ghi chú triển khai |
|---|---|---|---|
| FR-4.1 | Phân tích câu hỏi & truy xuất đoạn tài liệu liên quan (đã qua lọc bảo mật) | ✅ Đã có | `KnowledgePlugin` expose tool `memory` → KM search có filter |
| FR-4.2 | Grounding: ép AI chỉ trả lời dựa trên tài liệu, không bịa đặt | ⚠️ Một phần | Grounding bằng chỉ thị prompt (`BuildTextOnlyPrompt`). Là agentic RAG (AI tự quyết gọi tool), chưa phải retrieval bắt buộc |
| FR-4.3 | Thông báo khi không tìm thấy thông tin trong tài liệu | ✅ Đã có | Prompt yêu cầu báo "không trả lời được" hoặc dùng search online |
| FR-4.4 | Hỗ trợ nhiều nhà cung cấp LLM | ✅ Đã có | `AgentFactory` (GitHub Models, OpenAI, Azure OpenAI, Gemini) |

**Cần cải thiện (FR-4.2):** Cân nhắc retrieval bắt buộc trước khi sinh câu trả lời, hoặc kiểm tra hậu kiểm (grounding check) để giảm hallucination.

---

## 5. Module Quản lý Ngữ cảnh & Lịch sử trò chuyện (Context & Chat History Management)

| ID | Yêu cầu | Trạng thái | Ghi chú triển khai |
|---|---|---|---|
| FR-5.1 | Lưu trữ luồng hội thoại (hỏi nối tiếp) | ✅ Đã có | `Conversation` / `PChatMessage` |
| FR-5.2 | Cắt tỉa/tóm tắt ngữ cảnh khi hội thoại dài | ✅ Đã có | `PrepareConversationHistory`: giữ 5 tin mới nhất, tóm tắt phần cũ thành 1 summary message |
| FR-5.3 | Tối ưu chi phí token theo độ dài ngữ cảnh | ✅ Đã có | Cơ chế summary + theo dõi token (`TokenUsage`) |
| FR-5.4 | Bộ nhớ dài hạn của người dùng (Long-Term Memory) | ✅ Đã có | `IUserMemoryService`, cấu hình `LongTermMemory` |

---

## 6. Module Tương tác Người dùng (UI/UX & User Interaction)

| ID | Yêu cầu | Trạng thái | Ghi chú triển khai |
|---|---|---|---|
| FR-6.1 | Phản hồi theo thời gian thực (streaming) | ✅ Đã có | Streaming token-by-token (`ChatStreamingAsync`) |
| FR-6.2 | Minh bạch quá trình suy luận của AI | ✅ Đã có | Hiển thị `ThinkingContent`/reasoning trong `Chat.razor` |
| FR-6.3 | Thu thập phản hồi người dùng (thích/không thích) | ✅ Đã có | `ReactionType` (Like/Dislike) + nút thumbs |
| FR-6.4 | Trích dẫn nguồn dạng link về tài liệu gốc | ❌ Chưa có | `SearchResult` có dữ liệu nguồn nhưng UI **chưa render link** trích dẫn bấm được |
| FR-6.5 | Hiển thị từ khóa/câu truy vấn AI dùng để tìm kiếm | ❌ Chưa có | Tool-call arguments chưa được surface ra UI |

**Cần triển khai:**
- **FR-6.4:** Render `SearchResult.Results[].Partitions` thành danh sách nguồn có link tới tài liệu/trang gốc (dùng `ExportDocumentAsync` để mở tài liệu).
- **FR-6.5:** Hiển thị query mà AI truyền vào tool `memory` để người dùng thấy quá trình tìm kiếm.

---

## 7. Module Quản trị và Giám sát (Admin Dashboard & Audit)

| ID | Yêu cầu | Trạng thái | Ghi chú triển khai |
|---|---|---|---|
| FR-7.1 | Quản lý ánh xạ vai trò ↔ quyền truy cập tài liệu | ✅ Đã có | `TagManagement.razor` (mapping Tag ↔ Role) |
| FR-7.2 | Quản lý agent, tài liệu, thư mục, người dùng | ✅ Đã có | Admin portal (Blazor + YARP BFF) |
| FR-7.3 | Ghi nhật ký kiểm toán (audit) toàn bộ Q&A | ⚠️ Một phần | Q&A lưu trong DB + `TokenUsage`, nhưng **chưa có** trang/log audit chuyên dụng |
| FR-7.4 | Dashboard xem & lọc phản hồi tiêu cực để cải thiện | ❌ Chưa có | Reaction đã lưu DB nhưng chưa có giao diện admin review |
| FR-7.5 | Phát hiện tài liệu lỗi thời từ phản hồi tiêu cực | ❌ Chưa có | Phụ thuộc FR-7.4 |

**Cần triển khai:**
- **FR-7.3:** Bổ sung bảng/audit log chuyên dụng (user, câu hỏi, câu trả lời, agent, timestamp) phục vụ truy vết.
- **FR-7.4 / FR-7.5:** Trang admin tổng hợp các câu trả lời bị Dislike, liên kết tới tài liệu nguồn để rà soát & cập nhật kho kiến thức.

---

## Tổng hợp khoảng trống (Gap Summary)

| Ưu tiên | Hạng mục | Module |
|---|---|---|
| Cao | Trích dẫn nguồn dạng link trên UI (FR-6.4) | 6 |
| Cao | Dashboard audit Q&A (FR-7.3) | 7 |
| Trung bình | Dashboard review phản hồi tiêu cực (FR-7.4, FR-7.5) | 7 |
| Trung bình | Hiển thị từ khóa tìm kiếm của AI (FR-6.5) | 6 |
| Thấp | Tăng cường grounding chống hallucination (FR-4.2) | 4 |

> **Không trong phạm vi:** Multi-tenancy (cách ly dữ liệu đa tổ chức).
