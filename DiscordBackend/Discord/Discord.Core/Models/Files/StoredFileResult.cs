using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Core.Models.Files;

public sealed record StoredFileResult(
    string FileName,
    string StoredFileName,
    string FileUrl,
    string ContentType,
    long FileSize);