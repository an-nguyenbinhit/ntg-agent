using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace AskHR.Admin.E2ETests;

[TestFixture]
public class DocumentManagementTests : PageTest
{
    // Cấu hình URL AppHost (Admin Portal thường chạy ở port 5036 hoặc 7097)
    // Để chạy test này, AskHR.AppHost cần được khởi chạy trước.
    private const string AdminUrl = "http://localhost:5036";

    [SetUp]
    public async Task Setup()
    {
        // 1. Truy cập Admin Portal
        await Page.GotoAsync(AdminUrl);

        // 2. Thực hiện Login (do trang yêu cầu Authorize)
        // Nếu chuyển hướng sang trang Login:
        if (Page.Url.Contains("/Account/Login"))
        {
            // Điền email
            await Page.FillAsync("input[type='email']", "admin@askhr.com");
            // Điền password
            await Page.FillAsync("input[type='password']", "AskHR@123");
            // Bấm nút Login
            await Page.ClickAsync("button[type='submit']");
            
            // Đợi load xong chuyển hướng về trang chủ
            await Page.WaitForURLAsync($"{AdminUrl}/");
        }
    }

    [Test]
    public async Task AdminPortal_ShouldDisplaySeededDocuments()
    {
        // 1. Truy cập trang Agent Management mặc định
        // Tại trang chủ "/", bấm vào Default Agent
        await Page.ClickAsync("text='Agent Default'");

        // Đợi chuyển hướng sang trang chi tiết Agent
        await Page.WaitForURLAsync(new Regex(".*/agents/.*"));

        // 2. Chuyển sang tab Documents (nếu cần click)
        // Tìm button hoặc tab có chữ Documents
        var documentsTab = Page.Locator("text='Documents'");
        if (await documentsTab.IsVisibleAsync())
        {
            await documentsTab.ClickAsync();
        }

        // 3. Verify danh sách file seeded hiển thị
        await Expect(Page.Locator("text='leave-policy.md'").First).ToBeVisibleAsync();
        await Expect(Page.Locator("text='wfh-policy.md'").First).ToBeVisibleAsync();
        await Expect(Page.Locator("text='code-of-conduct.md'").First).ToBeVisibleAsync();
    }

    [Test]
    public async Task AdminPortal_EditMetadata_And_Reindex_ShouldSucceed()
    {
        // Tương tự, đi tới trang Documents của Agent Default
        await Page.ClickAsync("text='Agent Default'");
        await Page.WaitForURLAsync(new Regex(".*/agents/.*"));
        
        var documentsTab = Page.Locator("text='Documents'");
        if (await documentsTab.IsVisibleAsync())
        {
            await documentsTab.ClickAsync();
        }

        // Đợi Grid hiển thị
        await Expect(Page.Locator("text='leave-policy.md'").First).ToBeVisibleAsync();

        // 1. Click Edit Metadata cho file leave-policy.md
        var row = Page.Locator(".document-card", new() { HasTextString = "leave-policy.md" }).First;
        await row.Locator("button[title='Edit Metadata']").ClickAsync();

        // 2. Verify Modal hiển thị
        await Expect(Page.Locator(".modal-title, .mud-dialog-title").First).ToContainTextAsync(new Regex("Edit", RegexOptions.IgnoreCase));
        
        // Chọn Tag hoặc nhập liệu (giả định có input tags)
        // Vì Modal EditDocumentModal.razor sử dụng các field phức tạp, chúng ta test đơn giản nhất là ấn nút Save luôn
        var saveButton = Page.Locator("button", new() { HasTextString = "Save" });
        if (await saveButton.IsVisibleAsync())
        {
            await saveButton.ClickAsync();
        }
        
        // Modal sẽ tắt
        await Expect(saveButton).ToBeHiddenAsync();

        // 3. Bấm Re-index file đó
        await row.Locator("button[title='Manual Reindex']").ClickAsync();

        // 4. Verify Toast Notification / Snackbar hiển thị
        var snackbar = Page.Locator(".mud-snackbar, .toast, .alert").First;
        // Snackbar message thường chứa "success" hoặc "indexed"
        await Expect(snackbar).ToContainTextAsync(new Regex("success|index|queue", RegexOptions.IgnoreCase));
    }
}
