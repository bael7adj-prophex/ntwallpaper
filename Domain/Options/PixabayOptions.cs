namespace NTWallpaper.Domain.Options;

/// <summary>Valid Pixabay parameter value sets used by the UI and query builder.</summary>
public static class PixabayOptions
{
    public static IReadOnlyList<(string Value, string Label)> Languages { get; } = new[]
    {
        ("cs", "Czech"), ("da", "Danish"), ("de", "German"), ("en", "English"), ("es", "Spanish"),
        ("fr", "French"), ("id", "Indonesian"), ("it", "Italian"), ("hu", "Hungarian"), ("nl", "Dutch"),
        ("no", "Norwegian"), ("pl", "Polish"), ("pt", "Portuguese"), ("ro", "Romanian"), ("sk", "Slovak"),
        ("fi", "Finnish"), ("sv", "Swedish"), ("tr", "Turkish"), ("vi", "Vietnamese"), ("th", "Thai"),
        ("bg", "Bulgarian"), ("ru", "Russian"), ("el", "Greek"), ("ja", "Japanese"), ("ko", "Korean"),
        ("zh", "Chinese")
    };

    public static IReadOnlyList<(string Value, string Label)> Categories { get; } = new[]
    {
        ("backgrounds", "Backgrounds"), ("fashion", "Fashion"), ("nature", "Nature"), ("science", "Science"),
        ("education", "Education"), ("feelings", "Feelings"), ("health", "Health"), ("people", "People"),
        ("religion", "Religion"), ("places", "Places"), ("animals", "Animals"), ("industry", "Industry"),
        ("computer", "Computer"), ("food", "Food"), ("sports", "Sports"), ("transportation", "Transportation"),
        ("travel", "Travel"), ("buildings", "Buildings"), ("business", "Business"), ("music", "Music")
    };

    public static IReadOnlyList<(string Value, string Label)> Colors { get; } = new[]
    {
        ("grayscale", "Grayscale"), ("transparent", "Transparent"), ("red", "Red"), ("orange", "Orange"),
        ("yellow", "Yellow"), ("green", "Green"), ("turquoise", "Turquoise"), ("blue", "Blue"),
        ("lilac", "Lilac"), ("pink", "Pink"), ("white", "White"), ("gray", "Gray"), ("black", "Black"),
        ("brown", "Brown")
    };

    public static IReadOnlyList<(string Value, string Label)> Orders { get; } = new[]
    {
        ("popular", "Popular"), ("latest", "Latest")
    };

    public static string ToApiString(this Domain.Enums.ImageType value) => value switch
    {
        Domain.Enums.ImageType.All => "all",
        Domain.Enums.ImageType.Photo => "photo",
        Domain.Enums.ImageType.Illustration => "illustration",
        Domain.Enums.ImageType.Vector => "vector",
        _ => "all"
    };

    public static string ToApiString(this Domain.Enums.Orientation value) => value switch
    {
        Domain.Enums.Orientation.All => "all",
        Domain.Enums.Orientation.Horizontal => "horizontal",
        Domain.Enums.Orientation.Vertical => "vertical",
        _ => "all"
    };
}
