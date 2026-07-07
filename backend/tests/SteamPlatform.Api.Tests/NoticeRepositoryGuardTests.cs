using SteamPlatform.Application.Notices;
using SteamPlatform.Infrastructure.Notices;

namespace SteamPlatform.Api.Tests;

public sealed class NoticeRepositoryGuardTests
{
    [Theory]
    [MemberData(nameof(InvalidCreateRequests))]
    public async Task Create_rejects_invalid_fields_before_opening_database(CreateNoticeRequest request, string expectedMessage)
    {
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            new NoticeRepository(new ThrowingConnectionFactory()).CreateAsync(request, CancellationToken.None));

        Assert.StartsWith(expectedMessage, exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(InvalidUpdateRequests))]
    public async Task Update_rejects_invalid_fields_before_opening_database(string? noticeId, UpdateNoticeRequest request, string expectedMessage)
    {
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            new NoticeRepository(new ThrowingConnectionFactory()).UpdateAsync(noticeId!, request, CancellationToken.None));

        Assert.StartsWith(expectedMessage, exception.Message, StringComparison.Ordinal);
    }

    public static TheoryData<CreateNoticeRequest, string> InvalidCreateRequests() =>
        new()
        {
            { new CreateNoticeRequest(null, "ADM001", "Title", "Content", 1, null), "PublisherType is required." },
            { new CreateNoticeRequest("PLAYER", "P001", "Title", "Content", 1, null), "PublisherType must be SYSTEM, ADMIN or DEVELOPER." },
            { new CreateNoticeRequest("SYSTEM", "ADM001", "Title", "Content", 1, null), "System notices must not specify PublisherId." },
            { new CreateNoticeRequest("ADMIN", null, "Title", "Content", 1, null), "PublisherId is required for ADMIN and DEVELOPER notices." },
            { new CreateNoticeRequest("ADMIN", "ADM001", " ", "Content", 1, null), "Title is required." },
            { new CreateNoticeRequest("ADMIN", "ADM001", "Title", " ", 1, null), "Content is required." },
            { new CreateNoticeRequest("ADMIN", "ADM001", "Title", "Content", -1, null), "Priority must be between 0 and 9." },
            { new CreateNoticeRequest("ADMIN", "ADM001", "Title", "Content", 10, null), "Priority must be between 0 and 9." }
        };

    public static TheoryData<string?, UpdateNoticeRequest, string> InvalidUpdateRequests() =>
        new()
        {
            { null, new UpdateNoticeRequest("Title", "Content", 1, "PUBLISHED", null), "noticeId is required." },
            { " ", new UpdateNoticeRequest("Title", "Content", 1, "PUBLISHED", null), "noticeId is required." },
            { "N001", new UpdateNoticeRequest(" ", "Content", 1, "PUBLISHED", null), "Title is required." },
            { "N001", new UpdateNoticeRequest("Title", " ", 1, "PUBLISHED", null), "Content is required." },
            { "N001", new UpdateNoticeRequest("Title", "Content", 1, "OFFLINE", null), "Notice status must be DRAFT, PUBLISHED, EXPIRED or REVOKED." },
            { "N001", new UpdateNoticeRequest("Title", "Content", -1, "PUBLISHED", null), "Priority must be between 0 and 9." },
            { "N001", new UpdateNoticeRequest("Title", "Content", 10, "PUBLISHED", null), "Priority must be between 0 and 9." }
        };
}
