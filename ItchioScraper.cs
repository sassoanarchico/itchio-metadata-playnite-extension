using HtmlAgilityPack;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace ItchioMetadata
{
    public class ItchioSearchResult
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public string Description { get; set; }
        public string ThumbnailUrl { get; set; }
        public string Author { get; set; }
    }

    public class ItchioGameMetadata
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<string> Developers { get; set; }
        public List<string> Publishers { get; set; }
        public List<string> Genres { get; set; }
        public List<string> Tags { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public string CoverImageUrl { get; set; }
        public List<string> Screenshots { get; set; }
        public List<Link> Links { get; set; }
        public int? CommunityScore { get; set; }
        public string GameUrl { get; set; }
    }

    public class ItchioScraper
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private readonly HttpClient httpClient;

        private const string SearchUrlTemplate = "https://itch.io/search?q={0}";
        private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

        public ItchioScraper()
        {
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            httpClient = new HttpClient(handler);
            httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
            httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
            httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");
            httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public List<ItchioSearchResult> SearchGames(string searchTerm)
        {
            var results = new List<ItchioSearchResult>();

            try
            {
                string searchUrl = string.Format(SearchUrlTemplate, HttpUtility.UrlEncode(searchTerm));
                string html = GetHtmlContent(searchUrl);

                if (string.IsNullOrEmpty(html))
                    return results;

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var gameCells = doc.DocumentNode.SelectNodes("//div[contains(@class, 'game_cell')]");

                if (gameCells == null)
                    return results;

                foreach (var cell in gameCells.Take(20)) // Limit to 20 results
                {
                    try
                    {
                        var result = ParseSearchResult(cell);
                        if (result != null)
                        {
                            results.Add(result);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Warn(ex, "Failed to parse search result");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to search itch.io");
            }

            return results;
        }

        private ItchioSearchResult ParseSearchResult(HtmlNode cell)
        {
            var titleLink = cell.SelectSingleNode(".//a[contains(@class, 'title') or contains(@class, 'game_link')]");
            if (titleLink == null)
            {
                titleLink = cell.SelectSingleNode(".//div[@class='game_title']//a");
            }
            if (titleLink == null)
            {
                titleLink = cell.SelectSingleNode(".//a[@class='game_link']");
            }

            string title = titleLink?.InnerText?.Trim();
            string url = titleLink?.GetAttributeValue("href", null);

            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(url))
            {
                // Try alternative structure
                var gameTitle = cell.SelectSingleNode(".//div[contains(@class, 'game_title')]");
                if (gameTitle != null)
                {
                    var link = gameTitle.SelectSingleNode(".//a");
                    title = link?.InnerText?.Trim() ?? gameTitle.InnerText?.Trim();
                    url = link?.GetAttributeValue("href", null);
                }
            }

            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(url))
                return null;

            var result = new ItchioSearchResult
            {
                Title = HttpUtility.HtmlDecode(title),
                Url = url
            };

            // Get author
            var authorNode = cell.SelectSingleNode(".//div[contains(@class, 'game_author')]//a") ??
                            cell.SelectSingleNode(".//a[contains(@class, 'user_link')]");
            if (authorNode != null)
            {
                result.Author = HttpUtility.HtmlDecode(authorNode.InnerText?.Trim());
            }

            // Get description/text
            var textNode = cell.SelectSingleNode(".//div[contains(@class, 'game_text')]");
            if (textNode != null)
            {
                result.Description = HttpUtility.HtmlDecode(textNode.InnerText?.Trim());
            }

            // Get thumbnail
            var thumbNode = cell.SelectSingleNode(".//div[contains(@class, 'game_thumb')]//img") ??
                           cell.SelectSingleNode(".//img[contains(@class, 'lazy_loaded')]") ??
                           cell.SelectSingleNode(".//img");
            if (thumbNode != null)
            {
                result.ThumbnailUrl = thumbNode.GetAttributeValue("data-lazy_src", null) ??
                                     thumbNode.GetAttributeValue("src", null);
            }

            // Add author to description for display
            if (!string.IsNullOrEmpty(result.Author))
            {
                result.Description = $"by {result.Author}" +
                    (string.IsNullOrEmpty(result.Description) ? "" : $" - {result.Description}");
            }

            return result;
        }

        public ItchioGameMetadata GetGameMetadata(string gameUrl)
        {
            var metadata = new ItchioGameMetadata
            {
                GameUrl = gameUrl,
                Links = new List<Link>(),
                Screenshots = new List<string>(),
                Tags = new List<string>(),
                Genres = new List<string>(),
                Developers = new List<string>(),
                Publishers = new List<string>()
            };

            try
            {
                string html = GetHtmlContent(gameUrl);

                if (string.IsNullOrEmpty(html))
                    return metadata;

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                metadata.Links.Add(new Link("itch.io", gameUrl));

                ParseTitle(doc, metadata);
                ParseAuthor(doc, metadata);
                ParseDescription(doc, metadata);
                ParseCoverImage(doc, metadata);
                ParseScreenshots(doc, metadata);
                ParseTags(doc, metadata);
                ParseReleaseDate(doc, metadata);
                ParseRating(doc, metadata);
                ParseAdditionalLinks(doc, metadata);
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to get metadata from {gameUrl}");
            }

            return metadata;
        }

        private void ParseTitle(HtmlDocument doc, ItchioGameMetadata metadata)
        {
            var titleNode = doc.DocumentNode.SelectSingleNode("//h1[@class='game_title']") ??
                           doc.DocumentNode.SelectSingleNode("//h1[contains(@class, 'game_title')]") ??
                           doc.DocumentNode.SelectSingleNode("//div[@class='game_title']//h1") ??
                           doc.DocumentNode.SelectSingleNode("//title");

            if (titleNode != null)
            {
                string title = HttpUtility.HtmlDecode(titleNode.InnerText?.Trim());
                
                // strip "by Author" suffix if we got it from <title>
                if (titleNode.Name == "title" && title.Contains(" by "))
                {
                    title = title.Split(new[] { " by " }, StringSplitOptions.None)[0].Trim();
                }
                if (title.EndsWith(" - itch.io"))
                {
                    title = title.Substring(0, title.Length - " - itch.io".Length).Trim();
                }

                metadata.Name = title;
            }
        }

        private void ParseAuthor(HtmlDocument doc, ItchioGameMetadata metadata)
        {
            var authorNode = doc.DocumentNode.SelectSingleNode("//div[@class='game_author']//a") ??
                            doc.DocumentNode.SelectSingleNode("//a[contains(@class, 'user_link')]") ??
                            doc.DocumentNode.SelectSingleNode("//td[@class='game_info_panel_widget']//a[contains(@href, '.itch.io')]");

            if (authorNode != null)
            {
                string author = HttpUtility.HtmlDecode(authorNode.InnerText?.Trim());
                if (!string.IsNullOrEmpty(author))
                {
                    metadata.Developers.Add(author);
                    metadata.Publishers.Add(author);

                    string authorUrl = authorNode.GetAttributeValue("href", null);
                    if (!string.IsNullOrEmpty(authorUrl))
                    {
                        metadata.Links.Add(new Link("Developer Page", authorUrl));
                    }
                }
            }
        }

        private void ParseDescription(HtmlDocument doc, ItchioGameMetadata metadata)
        {
            var descNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'formatted_description')]") ??
                          doc.DocumentNode.SelectSingleNode("//div[@class='game_description']") ??
                          doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'page_widget')]//div[contains(@class, 'inner_column')]");

            if (descNode != null)
            {
                string description = descNode.InnerHtml;
                description = ConvertHtmlToDescription(description);
                
                metadata.Description = description?.Trim();
            }

            // fallback to meta description
            var shortDescNode = doc.DocumentNode.SelectSingleNode("//meta[@name='description']");
            if (shortDescNode != null && string.IsNullOrEmpty(metadata.Description))
            {
                metadata.Description = shortDescNode.GetAttributeValue("content", null);
            }
        }

        private string ConvertHtmlToDescription(string html)
        {
            if (string.IsNullOrEmpty(html))
                return null;

            html = Regex.Replace(html, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"</p>", "\n\n", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"</div>", "\n", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"</li>", "\n", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"<li[^>]*>", "• ", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"<h[1-6][^>]*>", "\n\n", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"</h[1-6]>", "\n", RegexOptions.IgnoreCase);

            html = Regex.Replace(html, @"<[^>]+>", "");
            html = HttpUtility.HtmlDecode(html);
            html = Regex.Replace(html, @"\n{3,}", "\n\n");
            html = Regex.Replace(html, @"[ \t]+", " ");

            return html.Trim();
        }

        private void ParseCoverImage(HtmlDocument doc, ItchioGameMetadata metadata)
        {
            var coverNode = doc.DocumentNode.SelectSingleNode("//div[@class='game_cover']//img") ??
                           doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'header')]//img[contains(@class, 'game_cover')]") ??
                           doc.DocumentNode.SelectSingleNode("//img[contains(@class, 'screenshot_image')]") ??
                           doc.DocumentNode.SelectSingleNode("//meta[@property='og:image']");

            if (coverNode != null)
            {
                string coverUrl = coverNode.Name == "meta" 
                    ? coverNode.GetAttributeValue("content", null)
                    : coverNode.GetAttributeValue("src", null) ?? coverNode.GetAttributeValue("data-lazy_src", null);

                if (!string.IsNullOrEmpty(coverUrl))
                {
                    if (coverUrl.StartsWith("//"))
                    {
                        coverUrl = "https:" + coverUrl;
                    }
                    metadata.CoverImageUrl = coverUrl;
                }
            }
        }

        private void ParseScreenshots(HtmlDocument doc, ItchioGameMetadata metadata)
        {
            var screenshotNodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'screenshot_container')]//a") ??
                                 doc.DocumentNode.SelectNodes("//div[contains(@class, 'screenshot_list')]//a") ??
                                 doc.DocumentNode.SelectNodes("//a[contains(@class, 'screenshot_link')]");

            if (screenshotNodes != null)
            {
                foreach (var node in screenshotNodes)
                {
                    string url = node.GetAttributeValue("href", null);
                    if (!string.IsNullOrEmpty(url) && (url.Contains(".png") || url.Contains(".jpg") || url.Contains(".jpeg") || url.Contains(".gif") || url.Contains(".webp")))
                    {
                        if (url.StartsWith("//"))
                        {
                            url = "https:" + url;
                        }
                        metadata.Screenshots.Add(url);
                    }
                }
            }

            var imgNodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'screenshot')]//img");
            if (imgNodes != null)
            {
                foreach (var node in imgNodes)
                {
                    string url = node.GetAttributeValue("src", null) ?? node.GetAttributeValue("data-lazy_src", null);
                    if (!string.IsNullOrEmpty(url) && !metadata.Screenshots.Contains(url))
                    {
                        if (url.StartsWith("//"))
                        {
                            url = "https:" + url;
                        }
                        if (!url.Contains("/50x50") && !url.Contains("/100x100"))
                        {
                            metadata.Screenshots.Add(url);
                        }
                    }
                }
            }

            if (!metadata.Screenshots.Any() && !string.IsNullOrEmpty(metadata.CoverImageUrl))
            {
                metadata.Screenshots.Add(metadata.CoverImageUrl);
            }
        }

        private void ParseTags(HtmlDocument doc, ItchioGameMetadata metadata)
        {
            var tagNodes = doc.DocumentNode.SelectNodes("//td[@class='game_info_panel_widget']//a[contains(@href, '/tag/')]") ??
                          doc.DocumentNode.SelectNodes("//a[contains(@href, '/games/tag-')]") ??
                          doc.DocumentNode.SelectNodes("//div[contains(@class, 'game_tags')]//a");

            if (tagNodes != null)
            {
                foreach (var node in tagNodes)
                {
                    string tag = HttpUtility.HtmlDecode(node.InnerText?.Trim());
                    if (!string.IsNullOrEmpty(tag))
                    {
                        metadata.Tags.Add(tag);
                    }
                }
            }

            var genreRow = doc.DocumentNode.SelectSingleNode("//tr[./td[contains(text(), 'Genre')]]") ??
                          doc.DocumentNode.SelectSingleNode("//div[contains(text(), 'Genre:')]");

            if (genreRow != null)
            {
                var genreLinks = genreRow.SelectNodes(".//a");
                if (genreLinks != null)
                {
                    foreach (var link in genreLinks)
                    {
                        string genre = HttpUtility.HtmlDecode(link.InnerText?.Trim());
                        if (!string.IsNullOrEmpty(genre))
                        {
                            metadata.Genres.Add(genre);
                        }
                    }
                }
            }

            // use tags as genres if none found
            if (!metadata.Genres.Any() && metadata.Tags.Any())
            {
                var genreKeywords = new[] { "action", "adventure", "rpg", "puzzle", "platformer", "shooter", 
                    "strategy", "simulation", "horror", "visual novel", "racing", "sports", "fighting",
                    "survival", "roguelike", "metroidvania", "sandbox", "open world" };

                foreach (var tag in metadata.Tags.ToList())
                {
                    if (genreKeywords.Any(g => tag.ToLower().Contains(g)))
                    {
                        metadata.Genres.Add(tag);
                    }
                }
            }
        }

        private void ParseReleaseDate(HtmlDocument doc, ItchioGameMetadata metadata)
        {
            var dateRow = doc.DocumentNode.SelectSingleNode("//tr[./td[contains(text(), 'Release date') or contains(text(), 'Published') or contains(text(), 'Updated')]]");
            
            if (dateRow != null)
            {
                var dateCell = dateRow.SelectSingleNode("./td[2]") ?? dateRow.SelectSingleNode("./td[last()]");
                if (dateCell != null)
                {
                    string dateText = dateCell.InnerText?.Trim();
                    metadata.ReleaseDate = ParseDate(dateText);
                }
            }

            if (!metadata.ReleaseDate.HasValue)
            {
                var abbrNode = doc.DocumentNode.SelectSingleNode("//abbr[@title]");
                if (abbrNode != null)
                {
                    string dateText = abbrNode.GetAttributeValue("title", null);
                    metadata.ReleaseDate = ParseDate(dateText);
                }
            }
        }

        private DateTime? ParseDate(string dateText)
        {
            if (string.IsNullOrEmpty(dateText))
                return null;

            string[] formats = new[]
            {
                "MMM d, yyyy",
                "MMMM d, yyyy",
                "d MMM yyyy",
                "d MMMM yyyy",
                "yyyy-MM-dd",
                "MM/dd/yyyy",
                "dd/MM/yyyy"
            };

            dateText = Regex.Replace(dateText, @"@.*$", "").Trim();

            foreach (var format in formats)
            {
                if (DateTime.TryParseExact(dateText, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
                {
                    return result;
                }
            }

            if (DateTime.TryParse(dateText, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime generalResult))
            {
                return generalResult;
            }

            return null;
        }

        private void ParseRating(HtmlDocument doc, ItchioGameMetadata metadata)
        {
            var ratingNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'aggregate_rating')]") ??
                            doc.DocumentNode.SelectSingleNode("//span[contains(@class, 'rating_value')]") ??
                            doc.DocumentNode.SelectSingleNode("//div[@itemprop='aggregateRating']//span[@itemprop='ratingValue']");

            if (ratingNode != null)
            {
                string ratingText = ratingNode.InnerText?.Trim();
                if (!string.IsNullOrEmpty(ratingText))
                {
                    // convert 5-star to 0-100
                    var match = Regex.Match(ratingText, @"([\d.]+)");
                    if (match.Success && double.TryParse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double rating))
                    {
                        if (rating <= 5)
                        {
                            metadata.CommunityScore = (int)(rating * 20);
                        }
                        else if (rating <= 100)
                        {
                            metadata.CommunityScore = (int)rating;
                        }
                    }
                }
            }

            var ratingContainer = doc.DocumentNode.SelectSingleNode("//*[@title and contains(@title, 'Rated')]");
            if (ratingContainer != null && !metadata.CommunityScore.HasValue)
            {
                string title = ratingContainer.GetAttributeValue("title", "");
                var match = Regex.Match(title, @"Rated\s+([\d.]+)\s+out\s+of\s+5");
                if (match.Success && double.TryParse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double rating))
                {
                    metadata.CommunityScore = (int)(rating * 20);
                }
            }
        }

        private void ParseAdditionalLinks(HtmlDocument doc, ItchioGameMetadata metadata)
        {
            var linkNodes = doc.DocumentNode.SelectNodes("//td[@class='game_info_panel_widget']//a[contains(@href, 'http') and not(contains(@href, 'itch.io'))]") ??
                           doc.DocumentNode.SelectNodes("//div[contains(@class, 'links')]//a[contains(@href, 'http')]");

            if (linkNodes != null)
            {
                foreach (var node in linkNodes)
                {
                    string url = node.GetAttributeValue("href", null);
                    string text = HttpUtility.HtmlDecode(node.InnerText?.Trim());

                    if (!string.IsNullOrEmpty(url) && !url.Contains("itch.io"))
                    {
                        string linkName = text;
                        if (url.Contains("twitter.com") || url.Contains("x.com"))
                            linkName = "Twitter";
                        else if (url.Contains("discord"))
                            linkName = "Discord";
                        else if (url.Contains("github.com"))
                            linkName = "GitHub";
                        else if (url.Contains("youtube.com"))
                            linkName = "YouTube";
                        else if (url.Contains("steam"))
                            linkName = "Steam";
                        else if (string.IsNullOrEmpty(linkName))
                            linkName = "Website";

                        if (!metadata.Links.Any(l => l.Url == url))
                        {
                            metadata.Links.Add(new Link(linkName, url));
                        }
                    }
                }
            }
        }

        private string GetHtmlContent(string url)
        {
            try
            {
                var task = Task.Run(() => httpClient.GetStringAsync(url));
                task.Wait();
                return task.Result;
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to fetch URL: {url}");
                return null;
            }
        }
    }
}
