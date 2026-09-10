using UniversalSoundboard.Common;

int checks = 0;
void Check(bool condition, string description)
{
    if (!condition) throw new Exception("FAILED: " + description);
    checks++;
}

const string id = "aB_cD-12345";
foreach (string url in new[] {
    $"https://youtu.be/{id}?si=shareToken",
    $"https://youtu.be/{id}?t=30#fragment",
    $"https://www.youtube.com/watch?v={id}&si=shareToken",
    $"https://www.youtube.com/watch?si=shareToken&v={id}&t=30",
    $"https://music.youtube.com/watch?v={id}",
    $"https://m.youtube.com/watch?v={id}",
    $"https://www.youtube.com/shorts/{id}?si=shareToken",
    $"https://youtube.com/live/{id}",
    $"https://youtube.com/embed/{id}",
    $"youtu.be/{id}",
    $"  HTTPS://YOUTU.BE/{id}  " })
{
    Check(YoutubeUrl.TryParse(url, out var actual, out var playlist) && actual == id && playlist == null,
        "valid URL: " + url);
}

Check(YoutubeUrl.TryParse($"https://youtube.com/watch?v={id}&list=PL_123-abc&index=2", out _, out var list)
    && list == "PL_123-abc", "playlist survives normalization");
foreach (var url in new[] {
    "", " ", null, "https://youtube.com/watch?v=", "https://youtu.be/short",
    $"https://youtu.be/{id}EXTRA", $"https://youtube.com.evil.test/watch?v={id}",
    $"https://notyoutube.com/watch?v={id}", $"ftp://youtu.be/{id}",
    $"https://youtu.be/{id}/extra", $"https://youtube.com/watch?v={id}%3Fsi%3Dx",
    "https://youtube.com/playlist?list=PL123", "https://youtube.com/shorts/",
    "https://youtube.com/watch?v=abc!2345678" })
{
    Check(!YoutubeUrl.TryParse(url, out _, out _), "reject incomplete/invalid URL: " + url);
}
for (int length = 0; length < id.Length; length++)
    Check(!YoutubeUrl.TryParse("https://youtu.be/" + id.Substring(0, length), out _, out _),
        "typing a partial video ID does not start a request");

// A second initialization must wait for the first, not return as if it had completed.
var gate = new AsyncOperationGate();
var entered = new TaskCompletionSource();
var release = new TaskCompletionSource();
var order = new List<int>();
var first = gate.RunAsync(async () => { order.Add(1); entered.SetResult(); await release.Task; order.Add(2); });
await entered.Task;
var second = gate.RunAsync(() => { order.Add(3); return Task.CompletedTask; });
Check(!second.IsCompleted && order.SequenceEqual(new[] { 1 }), "second initialization waits");
release.SetResult();
await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(5));
Check(order.SequenceEqual(new[] { 1, 2, 3 }), "initializations do not overlap");

var failedEntered = new TaskCompletionSource();
var failNow = new TaskCompletionSource();
var original = new InvalidOperationException("initialization failed");
var failed = gate.RunAsync(async () => { failedEntered.SetResult(); await failNow.Task; throw original; });
await failedEntered.Task;
bool retried = false;
var retry = gate.RunAsync(() => { retried = true; return Task.CompletedTask; });
Check(!retried, "retry waits for the failed attempt");
failNow.SetResult();
try { await failed; throw new Exception("Expected original exception"); }
catch (InvalidOperationException exception) { Check(ReferenceEquals(exception, original), "original exception preserved"); }
await retry.WaitAsync(TimeSpan.FromSeconds(5));
Check(retried, "failure releases the gate for retry");
try { await gate.RunAsync(() => throw original); }
catch (InvalidOperationException) { }
await gate.RunAsync(() => Task.CompletedTask).WaitAsync(TimeSpan.FromSeconds(5));
Check(true, "synchronous failure also releases the gate");
// Exercise the real Sentry event enrichment without initializing a client or sending anything.
Exception audioFailure;
try { throw new InvalidOperationException("audio initialization test"); }
catch (Exception exception) { audioFailure = exception; }
audioFailure.Data["audio.stage"] = "AudioGraph";
audioFailure.Data["audio.status"] = "DeviceNotAvailable";
AudioDiagnostics.Annotate(audioFailure, "Initialize", 2, ".wav");
var audioEvent = AudioDiagnostics.EnrichEvent(new Sentry.SentryEvent(audioFailure));
Check(audioEvent.Tags["audio.stage"] == "AudioGraph", "specific failing stage preserved");
Check(audioEvent.Tags["audio.status"] == "DeviceNotAvailable", "native status is queryable");
Check(audioEvent.Tags["audio.output_count"] == "2", "output count attached");
Check(audioEvent.Tags["audio.file_type"] == ".wav", "file type attached without path");
Check(audioEvent.Extra["audio.exception_details"].ToString().Contains("Program"), "original exception trace preserved");
Check(audioEvent.Extra.ContainsKey("audio.capture_stack"), "capture-site fallback labelled separately");
var regularEvent = new Sentry.SentryEvent(new Exception("unrelated"));
Check(ReferenceEquals(AudioDiagnostics.EnrichEvent(regularEvent), regularEvent)
    && !regularEvent.Extra.ContainsKey("audio.exception_details"), "unrelated errors unchanged");
Check(ShareFileName.Create("Artist: Song / Live?", "mp3") == "Artist_ Song _ Live_.mp3",
    "share replaces characters rejected by Windows");
Check(ShareFileName.Create("a<>:\"/\\|?*\u0000\n", ".wav") == "a___________.wav",
    "share handles all forbidden characters and control characters");
Check(ShareFileName.Create("Grüße 🎵", "ogg") == "Grüße 🎵.ogg", "share preserves Unicode titles");
foreach (string reserved in new[] { "CON", "nul", "AUX", "PRN", "COM1", "LPT9", "COM¹", "CON.mix" })
    Check(ShareFileName.Create(reserved, "wav").StartsWith("_"), "share escapes device name " + reserved);
Check(ShareFileName.Create("... ", null) == "Sound.mp3", "share supplies empty name and extension defaults");
Check(ShareFileName.Create("Title. ", "wav") == "Title.wav", "share removes trailing dots and spaces");
Check(ShareFileName.Create(new string('a', 300), "mp3").Length == 120, "share bounds long names");
Check(ShareFileName.Create(new string('a', 115) + "🎵", "mp3") == new string('a', 115) + ".mp3",
    "share truncation preserves surrogate pairs");
Check(ShareFileName.Create("Sound", "../wav") == "Sound._wav", "extension cannot introduce a path");
PitchQualityChecks.Run(Check);
Console.WriteLine($"Passed {checks} regression checks.");
