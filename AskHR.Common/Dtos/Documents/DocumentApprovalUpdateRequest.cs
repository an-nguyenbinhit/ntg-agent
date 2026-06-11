namespace AskHR.Common.Dtos.Documents;

public record DocumentApprovalUpdateRequest(ApprovalStatus ApprovalStatus, DateTime? NextReviewDate = null);
