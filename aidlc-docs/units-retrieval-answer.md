---
type: uob
uob: ["01", "02"]
status: draft
owner: HR / Eng
tags: [rag, rbac, slack, governance, security]
related: ["[[requirements]]", "[[units-security-identity#UoB-04: RBAC / Identity & Access]]", "[[units-channels#UoB-03: Slack Mention & Thread Context]]", "[[units-governance-ops#UoB-05: Admin Portal / Monitoring]]", "[[units-governance-ops#UoB-06: Feedback, Audit & Analytics]]", "[[ADR-001-agent-runtime]]"]
created: 2026-06-06
updated: 2026-06-07
---

# Units — Retrieval & Answer

> Nhóm UoB lõi RAG của AskHR: trả lời câu hỏi policy có grounding/citation (UoB-01) và ingest/chunk/index corpus HR kèm permission metadata (UoB-02). Hợp nhất từ hai file UoB gốc, **giữ nguyên nội dung**; mỗi UoB là một section H2.

## Mục lục

- [[#UoB-01: Answer Policy Question]]
- [[#UoB-02: Ingest & Index HR Documents]]


## UoB-01: Answer Policy Question

> **AI-DLC Inception artifact.** UoB này định nghĩa cách AskHR trả lời câu hỏi policy bằng corpus HR đã được ingest, có RBAC, grounding, citation và fallback khi thiếu nguồn.

### 1. Overview

HR cần một AI assistant trả lời câu hỏi về policy, benefit, FAQ, onboarding và quy trình nội bộ **chỉ dựa trên tài liệu HR cung cấp**. Nếu corpus không có nguồn đủ mạnh, bot không đoán và hướng dẫn user liên hệ HR.

Business outcome: nhân viên nhận câu trả lời nhanh, có nguồn kiểm chứng; HR giữ quyền kiểm soát nội dung và giảm rủi ro bot bịa chính sách.

### 2. Scope

| Nhóm | Nội dung |
|---|---|
| In scope | Câu hỏi có grounding trong corpus HR đã ingest: policy, leave, benefits, onboarding, quy trình nội bộ. |
| Out of scope | Câu hỏi không có nguồn trong corpus, câu hỏi ngoài HR, legal/medical advice, salary cá nhân, performance review. |
| Explicit non-goals | Không web search, không dùng general knowledge của model, không trả lời bằng suy đoán khi thiếu context. |
| Security boundary | Retrieval phải được security-trimmed bằng Authorization Context từ [[units-security-identity#UoB-04: RBAC / Identity & Access]]. |

### 3. Actors

| Actor | Mô tả | Nhu cầu chính |
|---|---|---|
| User | Nhân viên hỏi qua Slack hoặc Web Chat. | Câu trả lời nhanh, đúng quyền, dễ hiểu và có citation. |
| HR Admin | Quản lý corpus, persona và thông tin fallback HR. | Tin tưởng bot không bịa policy; kiểm soát được phạm vi trả lời. |
| HR Knowledge Owner | Review tài liệu, feedback và câu hỏi out-of-scope. | Biết gap tài liệu để bổ sung/cập nhật corpus. |

### 4. Preconditions

- [[units-retrieval-answer#UoB-02: Ingest & Index HR Documents]] đã tạo knowledge store với chunk, citation metadata và permission metadata.
- [[units-security-identity#UoB-04: RBAC / Identity & Access]] resolve được Authorization Context cho request.
- HR đã cấu hình HR contact/fallback channel.
- Prompt style/persona đã được cấu hình nhưng không được phép override grounding behavior.

### 5. Main Flow

1. User gửi câu hỏi qua Slack/Web Chat.
2. Channel gateway chuẩn hóa request: question, conversation/thread context, channel profile và Authorization Context.
3. RAG Pipeline áp metadata pre-filter theo role/tag/BU/sensitivity trước khi search.
4. Retrieval lấy top-k chunks bằng hybrid/vector search và loại chunk dưới similarity threshold.
5. Answer generator tạo câu trả lời chỉ từ retrieved chunks hợp lệ.
6. Bot gắn citation cho các claim quan trọng: policy name, section, source link hoặc document id.
7. Nếu answer không đạt citation/grounding contract, bot fallback thay vì trả lời đoán.
8. Channel gateway trả câu trả lời với tone thân thiện và metadata audit cho [[units-governance-ops#UoB-06: Feedback, Audit & Analytics]].

### 6. Alternate Flows

| Case | Trigger | Expected behavior |
|---|---|---|
| Out-of-scope | Không có chunk đủ similarity hoặc không gắn được citation. | Từ chối lịch sự, hiển thị HR contact, log masked question + hash. |
| Sensitive / out-of-authority | Quấy rối, sức khỏe tâm lý, xung đột nội bộ, khiếu nại nhân viên. | Freeze auto-answer; warm handoff context cho HR Advisor; không đưa lời khuyên; sinh handoff event cho [[units-governance-ops#UoB-06: Feedback, Audit & Analytics]]. |
| Unauthorized content | Có tài liệu liên quan nhưng bị loại bởi RBAC filter. | Fallback như no-source; không tiết lộ rằng tài liệu tồn tại. |
| Ambiguous question | Query thiếu intent, ví dụ "Tôi muốn nghỉ". | Hỏi lại một câu ngắn để phân biệt nghỉ phép, nghỉ bệnh hay nghỉ việc. |
| High-risk answer | Topic Benefit/Leave/quyền lợi, confidence thấp, conflict hoặc expired doc. | Bật optional groundedness check nếu config cho phép; nếu fail thì fallback/escalate. |
| Pipeline error | Timeout, provider error, vector search error. | Channel gateway dùng error UX của UoB-03/UoB-08 và notify admin theo config. |

### 7. Success Criteria

| Criteria | Measurement |
|---|---|
| In-scope accuracy | Golden Q&A hoặc HR spot-check xác nhận câu trả lời đúng nguồn. |
| Refusal accuracy | Không trả lời bừa khi no-source/out-of-scope; không từ chối nhầm câu hỏi có nguồn. |
| Citation coverage | Claim quan trọng có citation hợp lệ. |
| Security trimming | User không nhận chunk/citation ngoài role/tag/BU được phép. |
| Latency | Mục tiêu đề xuất: dưới 5-10 giây cho Slack/Web Chat; cần benchmark thực tế. |
| Tone | HR đánh giá phản hồi thân thiện, tự nhiên, không làm yếu grounding. |

### 8. Decisions

#### 8.1 HR Data Source

- Định dạng chính: `.docx` và `.md` text-based.
- PDF, SharePoint, Excel qua cùng pipeline ingest.
- HR là owner quản lý và cập nhật corpus.
- Cần thống nhất template tài liệu với HR ngay từ đầu để chunking không vỡ ngữ nghĩa.

#### 8.2 Grounding & Confidence

Mặc định dùng **Standard RAG có kiểm soát**, không bật nhiều lớp LLM judge trong default path.

| Layer | Trạng thái | Ghi chú |
|---|---|---|
| Similarity threshold | Required | Loại chunk dưới ngưỡng trước khi vào prompt. |
| Citation-required prompting | Required | Không cite được nguồn thì fallback. |
| Groundedness judge / NLI | Optional/gated | Chỉ bật cho high-risk, confidence thấp, conflict, expired doc hoặc khi benchmark đạt latency. |

Lý do: similarity + citation đủ cho phần lớn câu hỏi HR policy. LLM judge thêm một lượt model call, tăng latency/cost và không nên nằm trong default path.

#### 8.3 Channel Boundary

- Slack Bolt SDK self-host là default/ưu tiên trước.
- Business logic RAG không phụ thuộc Slack-specific type.
- Channel adapter chỉ chuẩn hóa request/response; core RAG xử lý `AskHrRequest` đã normalize.

#### 8.4 Re-index Trigger

- Không re-index theo cron cố định.
- Re-index khi có tài liệu mới/thay đổi: hash, timestamp hoặc HR trigger qua Admin UI.

#### 8.5 Out-of-Scope Logging & Privacy

- Lưu masked question text + `questionHash` để HR phân tích gap tài liệu.
- Raw text chỉ lưu khi HR/Legal approve.
- Masking tối thiểu: email, phone, employee id, person name nếu nhận diện được và free-text comment.
- Retention dùng config thống nhất với [[units-governance-ops#UoB-06: Feedback, Audit & Analytics]].

#### 8.6 Persona & Tone

- Admin có thể cấu hình style prompt, tone, creativity cap và danh sách emoji cho phép.
- Creativity chỉ ảnh hưởng văn phong, không ảnh hưởng retrieval, citation, fallback hoặc security trimming.

#### 8.7 Security-Trimmed Retrieval

- UoB-01 nhận Authorization Context từ [[units-security-identity#UoB-04: RBAC / Identity & Access]] cho mọi request.
- Vector search phải pre-filter metadata `allowedRoles`, `tags`, `businessUnit`, `sensitivity`.
- Prompt không phải security boundary.
- **Deny-by-default**: chunk thiếu metadata quyền hoặc không match quyền thì không được dùng.
- `level`/`applicableTo` là tín hiệu **relevance** (Advanced RAG) để chọn đúng biến thể policy theo cấp bậc user, **không thay thế** security trimming và không nới quyền.

### 9. Contracts

#### AuthorizationContext

```jsonc
{
  "userId": "internal-user-id",
  "roles": ["Employee"],
  "allowedTags": ["public-all", "vn-policy"],
  "businessUnits": ["Vietnam"],
  "countries": ["Vietnam"],
  "legalEntities": ["VN-Legal-Entity"],
  "level": "Staff",
  "isAnonymous": false
}
```

#### Answer Contract

```jsonc
{
  "answerText": "string",
  "citations": [
    {
      "documentId": "string",
      "documentName": "string",
      "section": "string",
      "sourceUrl": "string"
    }
  ],
  "confidence": 0.82,
  "fallbackReason": null,
  "auditMetadata": {
    "provider": "AzureOpenAI",
    "model": "string",
    "retrievalStrategy": "StandardRag"
  }
}
```

### 10. Dependencies

- [[units-retrieval-answer#UoB-02: Ingest & Index HR Documents]]: cung cấp chunk, embedding, citation metadata và permission metadata.
- [[units-channels#UoB-03: Slack Mention & Thread Context]]: Slack gateway, loading/error UX và admin notification.
- [[units-security-identity#UoB-04: RBAC / Identity & Access]]: identity resolution và Authorization Context.
- [[units-governance-ops#UoB-06: Feedback, Audit & Analytics]]: audit event, feedback, severity signal và retention/masking policy.
- [[ADR-001-agent-runtime]]: agent runtime (MAF) điều phối skill orchestration, session state và human-in-the-loop handoff; runtime là engine, **không** phải trust boundary — RBAC/citation vẫn enforce server-side.

### 11. Skill Model

> **Mục đích.** Biến *hành vi* trả lời thành đơn vị **HR cấu hình được** thay vì chôn trong một system prompt khổng lồ do developer giữ. Skill là lớp orchestration phía trên Standard RAG; nó **không** thay thế và **không** được phép làm yếu retrieval, citation, fallback hay security trimming.

#### 11.1 Skill là gì

Một **Skill** là một capability đóng gói gồm metadata (`description` làm trigger), instructions, answer policy, tài nguyên kèm theo (attachments, optional script) và escalation. Skill được HR/Knowledge Owner tác giả qua Admin Portal (xem [[units-governance-ops#UoB-05: Admin Portal / Monitoring]] §8.6), version hóa và approve trước khi active.

#### 11.2 Progressive Disclosure

Bot không nạp toàn bộ skill cùng lúc, mà theo 3 giai đoạn để tiết kiệm token và giữ instruction-following ổn định:

| Giai đoạn | Hành động | Tái dùng |
|---|---|---|
| Discovery | Chỉ đọc `name` + `description` của các skill enabled để biết skill nào có thể liên quan. | Intent Pre-Filter [[units-channels#UoB-03: Slack Mention & Thread Context]] §8.3. |
| Activation | Câu hỏi match → nạp full instructions/answer policy của đúng skill đó. | Persona/tone [[units-governance-ops#UoB-05: Admin Portal / Monitoring]]. |
| Execution | Chạy Standard RAG theo answer policy của skill; optional gọi script/attachment nếu skill khai báo. | Main Flow §5, optional judge §8.2. |

#### 11.3 Vị trí trong Main Flow

Skill selection chèn giữa bước 2 (normalize request) và bước 3 (retrieval) của [§5 Main Flow](#5-main-flow): skill set instructions + answer policy + scope, sau đó **đúng pipeline retrieval/citation/fallback chạy không đổi**. Nếu không skill nào match, dùng skill mặc định `policy-lookup` (tương đương hành vi Standard RAG).

#### 11.4 Skill Contract

```jsonc
{
  "skillId": "leave-policy",
  "name": "Leave Policy",
  "description": "Câu hỏi nghỉ phép/bệnh/thai sản/carry-over",
  "enabled": true,
  "owner": "HR-Knowledge-Owner",
  "approvalStatus": "Approved",
  "version": 3,
  "scope": {
    "topics": ["Leave"],
    "tags": ["public-all", "vn-policy"],
    "businessUnits": ["Vietnam"]
  },
  "instructions": "Luôn nêu số ngày + điều kiện thâm niên + effective date.",
  "answerPolicy": {
    "requireCitation": true,
    "refuseIfExpired": true,
    "clarifyingQuestions": ["Bạn đã làm đủ 12 tháng chưa?"]
  },
  "tools": ["annual-leave-calculator"],
  "attachments": ["leave-request-form.pdf"],
  "escalation": {
    "fallbackContact": "#hr-vn",
    "severityHint": "P1"
  }
}
```

#### 11.5 Ràng buộc bất biến

- Skill **không** override security trimming hay deny-by-default (§8.7); `scope` của skill chỉ thu hẹp thêm, không nới quyền ngoài Authorization Context.
- Skill **không** tắt được citation contract khi câu trả lời là claim policy; `requireCitation=false` chỉ áp cho skill non-policy (ví dụ greeting/chit-chat).
- `tools`/script chạy deterministic (ví dụ `annual-leave-calculator` cho test case calculation ở [[requirements]] §14); model không tự bịa số. Script là dev cung cấp, HR chỉ bật/tắt.
- Model route cho mỗi skill do [[units-governance-ops#UoB-07: Multi-Provider / Model Configuration]] quyết định; skill mô tả capability, không hardcode provider.

#### 11.6 Skill Set & Registry

Hệ thống cung cấp tối thiểu các skill curated: `policy-lookup` (default), `ambiguity-clarifier`, `escalate-to-hr`, `sensitive-handoff` (freeze auto-answer + warm handoff cho HR Advisor khi gặp chủ đề nhạy cảm/vượt thẩm quyền). Registry có boundary sạch để HR thêm/sửa skill **không cần developer**. Skill planner (chain nhiều skill) thuộc năng lực Agentic RAG (xem [[units-retrieval-answer#UoB-02: Ingest & Index HR Documents]] §9).

### Changelog

- Chuẩn hóa UoB theo template chung: Overview, Scope, Actors, Preconditions, Main Flow, Alternate Flows, Success Criteria, Decisions, Contracts, Dependencies.
- Làm rõ Standard RAG là default path; LLM groundedness judge chỉ là optional/gated path.
- Chuẩn hóa out-of-scope logging theo masked text + hash và link trực tiếp tới UoB-06.
- Thêm §11 Skill Model: Agent Skills là lớp config HR sở hữu, progressive disclosure, skill contract và ràng buộc bất biến (không làm yếu RBAC/citation).
- Chuyển sang dạng requirement: đổi tên Baseline RAG → Standard RAG; thêm trục `level` vào Authorization Context và ghi chú level-aware relevance (Advanced RAG) tách biệt access.
- Thêm xử lý **sensitive / out-of-authority**: alternate flow freeze + warm handoff và skill `sensitive-handoff`.



## UoB-02: Ingest & Index HR Documents

> **AI-DLC Inception artifact.** UoB này tạo corpus có cấu trúc cho AskHR: parse, chunk, embed, index và quản lý version/metadata của tài liệu HR.

### 1. Overview

UoB-02 xây dựng ingestion pipeline để HR đưa tài liệu policy, process, benefit và onboarding vào AskHR. Pipeline giữ cấu trúc tài liệu, tạo chunk/embedding và lưu vào knowledge store để [[units-retrieval-answer#UoB-01: Answer Policy Question]] retrieve.

Chất lượng ingest quyết định trực tiếp chất lượng câu trả lời, citation và khả năng "biết mình không biết" của RAG Pipeline.

### 2. Scope

| Nhóm | Nội dung |
|---|---|
| In scope | `.docx` và `.md`; parse heading/section; chunk semantic; embedding; vector store; metadata store; versioning; permission metadata. |
| Nguồn mở rộng | PDF, SharePoint, Excel và connector tự động qua cùng `IDocumentSource` và pipeline. |
| Explicit non-goals | Không paraphrase hoặc tự viết lại policy khi index; không index tài liệu Confidential/PII vào corpus. |
| Security boundary | Metadata quyền phải được gắn khi ingest để UoB-01 enforce bằng vector metadata filter. |

### 3. Actors

| Actor | Mô tả | Nhu cầu chính |
|---|---|---|
| HR Admin | Upload, replace, gán metadata, xem index status. | Tự vận hành knowledge base không cần developer. |
| HR Knowledge Owner | Review version, conflict, freshness, approval. | Đảm bảo policy active là bản đúng. |
| RAG Pipeline | Consumer của knowledge store. | Cần chunk đúng nghĩa, có citation và permission metadata. |

### 4. Preconditions

- HR thống nhất Structured Word template hoặc Markdown template tối thiểu.
- Admin Portal có flow upload/replace document.
- Vector store được chọn phải hỗ trợ filterable metadata.
- Mapping role/tag/BU từ [[units-security-identity#UoB-04: RBAC / Identity & Access]] đã có schema dùng chung.

### 5. Main Flow

1. HR Admin upload file `.docx` hoặc `.md` qua Admin UI.
2. Pipeline tính hash/timestamp để phát hiện tài liệu mới hoặc thay đổi.
3. Parser đọc heading, section, table và metadata bắt buộc.
4. Chunker cắt theo cấu trúc ngữ nghĩa; nếu section quá dài thì dùng fallback overlap.
5. Pipeline gắn metadata: document, section, version, owner, effective/expired date, source path, `allowedRoles`, `tags`, `businessUnit`, `country`, `legalEntity`, `sensitivity`, `level`/`applicableTo`.
6. Embedding service tạo vector cho từng chunk.
7. Vector store upsert active chunks và xóa/thay thế chunk cũ của cùng document/version active.
8. Metadata store lưu version record, ingest status, hash và audit info.
9. Admin UI hiển thị kết quả: số chunk, trạng thái, lỗi, warning hoặc conflict.

### 6. Alternate Flows

| Case | Trigger | Expected behavior |
|---|---|---|
| Invalid file | File hỏng, sai format hoặc thiếu heading/template bắt buộc. | Không index; báo lỗi rõ cho HR Admin. |
| Missing permission metadata | Document/chunk chưa có role/tag/BU. | Lưu trạng thái `NeedsPermissionMapping`; UoB-01 deny-by-default. |
| Duplicate/conflict | Chunk mới tương tự cao với document/version khác. | Tạo warning cho HR review; không tự xóa/ghi đè. |
| Re-index existing document | Hash thay đổi hoặc HR replace document. | Xóa/thay thế active chunks cũ, giữ version history theo retention. |
| Embedding model changed | Provider/model/dimension thay đổi. | Tạo index version mới; không trộn embedding space. |

### 7. Success Criteria

| Criteria | Measurement |
|---|---|
| Traceability | Mọi chunk truy vết được về document, section, version và source. |
| Citation readiness | Chunk có đủ metadata để UoB-01 cite policy/section/source. |
| Permission readiness | Chunk có metadata quyền hoặc bị quarantine/deny-by-default. |
| Re-index hygiene | Không tạo orphaned chunks hoặc song song nhiều active version. |
| HR operability | HR Admin upload/update/xem status không cần dev hỗ trợ. |

### 8. Decisions

#### 8.1 Document Source

- Hỗ trợ **Admin UI Upload**.
- Hỗ trợ `WatchFolder` cho location vận hành mà HR xác nhận.
- Cả hai source phải đi qua cùng `IDocumentSource` và cùng ingestion pipeline.

```jsonc
{
  "IngestionSource": {
    "Mode": "AdminUpload",
    "WatchPath": null,
    "PollingInterval": null
  }
}
```

#### 8.2 Chunking Strategy

| Strategy | Cách hoạt động | Khi dùng |
|---|---|---|
| `HeadingBased` | Cắt theo H2/H3 hoặc Word Heading style. | Default khi tài liệu theo template. |
| `FixedSizeOverlap` | Cắt theo token/character với overlap. | Fallback cho tài liệu không có cấu trúc tốt. |
| `Hybrid` | Heading-based làm khung; section dài thì fixed-size + overlap bên trong. | Khuyến nghị lâu dài. |

Mặc định dùng `HeadingBased` + fallback `Hybrid`. Overlap nhỏ 10-15% giúp không mất ngữ cảnh ở đầu/cuối section.

#### 8.3 Vector Store

- Default: **Azure AI Search**.
- Lý do: cùng Azure stack, tích hợp Azure OpenAI, hỗ trợ hybrid search và filterable metadata.
- Giữ interface `IVectorStore` để không khóa cứng vào SDK/provider cụ thể.
- Adapter dự phòng qua `IVectorStore`: `Postgres_pgvector`, `Qdrant`.

#### 8.4 Conflict Detection

- Post-ingest so sánh embedding similarity giữa chunk mới và chunk hiện có.
- Optional LLM arbitration chỉ dùng để phân loại `Duplicate`, `Supplement`, `Conflict`.
- Conflict tạo warning/review item cho HR; hệ thống không tự sửa policy.

```jsonc
{
  "ConflictDetection": {
    "Enabled": true,
    "SimilarityThreshold": 0.9,
    "UseLLMArbitration": false
  }
}
```

#### 8.5 Versioning & Audit

- Mỗi ingest tạo một version record: document id, version, hash, timestamp, uploader, status, optional diff summary.
- Vector store chỉ giữ active version để retrieve.
- Metadata store giữ history theo retention policy.

```jsonc
{
  "VersionRetention": {
    "Mode": "KeepLastN",
    "Value": 10
  }
}
```

#### 8.6 HR Document Template

Template chuẩn: **Structured Word template**.

| Template | Đặc điểm | Đánh giá |
|---|---|---|
| Markdown + YAML frontmatter | Dễ version bằng git/wiki, rõ metadata. | Tốt cho technical team, có thể khó với HR. |
| Structured Word template | Heading style cố định, metadata table, dễ dùng. | **Default đề xuất cho HR**. |
| Hybrid Word checklist | Linh hoạt hơn nhưng dễ lệch chuẩn. | Dùng khi HR cần chuyển tiếp. |

Kickoff action: dev tạo một `.docx` mẫu với Heading style và metadata bắt buộc, gửi HR approve. Hệ thống chỉ nhận tài liệu theo template này qua Admin UI Upload; tài liệu lệch template chuyển sang `NeedsNormalization`.

#### 8.7 Permission Metadata

| Field | Ý nghĩa | Nguồn gán |
|---|---|---|
| `allowedRoles[]` | Role được phép thấy chunk. | HR gán hoặc kế thừa folder/BU. |
| `tags[]` | Tag phân loại, gồm `public-all` nếu cho anonymous. | HR gán hoặc auto-tagging. |
| `businessUnit` | BU sở hữu tài liệu. | Suy từ folder/source hoặc HR chọn. |
| `country` | Quốc gia/location áp dụng hoặc được phép truy cập. | HR gán hoặc suy từ source folder. |
| `legalEntity` | Pháp nhân áp dụng khi policy khác nhau theo entity. | HR gán khi có nhiều legal entity. |
| `sensitivity` | `Public`, `Internal`, `Confidential`. | Default `Internal`; `Confidential` không index vào corpus. |
| `level` / `applicableTo` | Cấp bậc/đối tượng áp dụng của policy. Tín hiệu **relevance** cho Advanced RAG (rerank/filter), **không phải access**. | HR gán hoặc suy từ section. |

Chunk thiếu metadata quyền vẫn có thể lưu metadata record, nhưng không active cho retrieval cho tới khi được gán quyền. `level`/`applicableTo` là metadata relevance: thiếu nó không chặn retrieval nhưng làm yếu khả năng chọn đúng biến thể policy theo cấp bậc.

### 9. Retrieval Architecture

Knowledge store phải phục vụ kiến trúc retrieval **phân tầng** mà [[units-retrieval-answer#UoB-01: Answer Policy Question]] chọn theo loại câu hỏi. Mọi tầng bắt buộc security trimming, citation và fallback.

| Tầng | Yêu cầu với knowledge store |
|---|---|
| **Standard RAG** | Top-k hybrid/vector search + metadata filter + similarity threshold. |
| **Advanced RAG** | Thêm reranking (cross-encoder) trên top-k và level-aware relevance: chunk mang `level`/`applicableTo` để filter/boost đúng biến thể policy theo cấp bậc/đối tượng. |
| **Agentic RAG** | Hỗ trợ nhiều lượt retrieve và chain skill cho câu hỏi multi-step/multi-policy. |
| **Graph RAG** | Hỗ trợ truy vấn quan hệ policy/country/legal entity/level/employment type khi corpus có graph taxonomy. |

Standard path:

```text
Query
  -> Resolve identity / RBAC (access)
  -> Retrieve top-k chunks with metadata filter
  -> Apply similarity threshold
  -> (Advanced) Rerank + level-aware relevance (applicableTo)
  -> Generate answer with citation
  -> Fallback to HR contact if source is insufficient
```

#### Reranking & Level-Aware Relevance

- Reranking dùng **reranker model chuyên dụng (cross-encoder)**, không phải LLM-as-judge, để giữ latency.
- `level`/`applicableTo` là tín hiệu **relevance**, **tách biệt** với access của [[units-security-identity#UoB-04: RBAC / Identity & Access]]: khi user có quyền thấy nhiều biến thể policy, retrieval phải chọn đúng biến thể cho cấp bậc/đối tượng của user, không trộn mức.

#### Agentic & Graph RAG

- Agentic RAG điều phối nhiều lượt retrieve và chain skill (xem [[units-retrieval-answer#UoB-01: Answer Policy Question]] §11).
- Graph RAG được backed bởi **Knowledge Graph** kết hợp vector store (hybrid): KG mô hình hóa quan hệ thực thể (vd `Chính sách thưởng → Senior → Hà Nội`) để suy luận trên quan hệ policy theo country/legal entity/level/employment type; cần metadata đủ tốt và owner vận hành graph taxonomy.
- Việc kích hoạt mỗi tầng cho từng nhóm câu hỏi được đánh giá bằng golden-set và analytics từ [[units-governance-ops#UoB-06: Feedback, Audit & Analytics]].

### 10. Construction Contract

```csharp
public interface IRetrievalService
{
    Task<RetrievalResult> SearchAsync(
        string query,
        AuthorizationContext authorization,
        RetrievalOptions options,
        CancellationToken cancellationToken);
}

public interface IRetrievalStrategy
{
    string Name { get; }
    Task<RetrievalResult> SearchAsync(RetrievalRequest request, CancellationToken cancellationToken);
}

public sealed class StandardRagStrategy : IRetrievalStrategy
{
    public string Name => "StandardRag";
}

public sealed class AdvancedRagStrategy : IRetrievalStrategy
{
    // Standard + reranking (cross-encoder) + level-aware relevance.
    public string Name => "AdvancedRag";
}
```

Mọi tầng retrieval implement qua `IRetrievalStrategy`. `StandardRagStrategy` và `AdvancedRagStrategy` là bắt buộc; `AgenticRagStrategy` và `GraphRagStrategy` thêm qua cùng boundary. `IRetrievalService` chọn strategy theo loại câu hỏi và kết quả golden-set.

### 11. Dependencies

- [[units-retrieval-answer#UoB-01: Answer Policy Question]] tiêu thụ knowledge store, citation metadata và permission metadata.
- [[units-security-identity#UoB-04: RBAC / Identity & Access]] định nghĩa role/tag/BU schema dùng khi ingest và retrieve.
- [[units-governance-ops#UoB-05: Admin Portal / Monitoring]] cung cấp UI upload, status, version, freshness và approval queue.
- [[units-governance-ops#UoB-06: Feedback, Audit & Analytics]] tiêu thụ audit/version/conflict/freshness event.

### Changelog

- Chuẩn hóa UoB theo template chung và tách rõ Main Flow / Alternate Flows.
- Làm rõ Standard RAG là default retrieval path; Agentic RAG và Graph RAG đi qua cùng strategy boundary khi golden-set chứng minh cần thiết.
- Chuyển config và construction contract sang code block có language (`jsonc`, `csharp`, `text`).
- Cố định recommendation: Structured Word template + Admin UI Upload cho pilot.
- Chuyển sang dạng requirement, không dùng delivery-wave framing. §9 định nghĩa retrieval phân tầng Standard/Advanced/Agentic/Graph; thêm **Advanced RAG** (reranking cross-encoder + level-aware relevance) và metadata `level`/`applicableTo` (relevance, tách biệt access); đổi tên strategy `Baseline`→`Standard` và thêm `AdvancedRagStrategy`.
- Làm rõ Graph RAG được backed bởi **Knowledge Graph** (hybrid với vector store) kèm ví dụ Thưởng→Senior→Hà Nội.

## Changelog (Consolidation)

- 2026-06-07: Hợp nhất UoB-01 (Answer Policy Question) + UoB-02 (Ingest & Index HR Documents) vào `units-retrieval-answer.md`. Mỗi UoB là một section H2, sub-section demote một cấp, per-UoB Table of Contents thay bằng "Mục lục" ở đầu file, và wikilink cập nhật sang dạng `[[file#heading]]`. Nội dung nghiệp vụ giữ nguyên.
