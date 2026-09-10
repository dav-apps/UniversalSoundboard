using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;

namespace RichTextControls.Generators
{
    public class HtmlXamlGenerator : IHtmlXamlGenerator
    {
        private readonly HtmlParser _parser;
        private readonly string _html;
        private IHtmlDocument _document;

        private static readonly Regex _htmlWhitespaceRegex = new Regex(@"(?<=\s)\s+(?![^<pre>]*</pre>)", RegexOptions.Compiled);
        private static readonly Regex _preTagRegex = new Regex(@"(?:\<pre\>)(.*)(?:\<\/pre\>)", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

        public HtmlXamlGenerator(string html)
        {
            _parser = new HtmlParser();
            _html = PrepareRawHtml(html);
        }

        public HtmlXamlGenerator(IHtmlDocument document)
        {
            _document = document;
        }

        /// <summary>
        /// The <see cref="Style"/> to be applied to <see cref="Border"/> encapsulating everything between <blockquote></blockquote> HTML tags.
        /// </summary>
        public Style BlockquoteBorderStyle
        {
            get;
            set;
        }

        /// <summary>
        /// The <see cref="Style"/> to be applied to <see cref="Border"/> encapsulating everything between <pre></pre> HTML tags.
        /// </summary>
        public Style PreformattedBorderStyle
        {
            get;
            set;
        }

        /// <summary>
        /// Parses the HTML and generates elements from the children.
        /// </summary>
        /// <returns>A <see cref="UIElement"/> representing the HTML.</returns>
        /// <exception cref="InvalidOperationException">Thrown when no parser is detected. Typically occurs when subclassed without calling base constructor.</exception>
        private RichTextBlock richTextBlock;
        public UIElement Generate()
        {
            if (_parser == null && _document == null)
                throw new InvalidOperationException("No HTML parser was set. If this is a subclass you must instantiate the parent with `base()`.");

            _document = _document ?? _parser.ParseDocument(_html);

            richTextBlock = new RichTextBlock();

            AddChildren(_document.Body, richTextBlock.Blocks);

            return richTextBlock;
        }

        /// <summary>
        /// Creates <see cref="Inline"/> elements which are ones that can be nested inside of a <p> tag:
        /// <acronym>, <b>, <big>, <br/>, <button>, <cite>, <code>, <dfn>, <em>, <font>, <i>, <iframe>, <img>, <input>, <kbd>, <label>, <q>, <s>, <samp>, <script>, <select>, <small>, <span>, <strike>, <strong>, <sub>, <sup>, <textarea>, <tt>, <u>, <var>
        /// </summary>
        /// <param name="node">The <see cref="INode"/> to generate the <see cref="Inline"/> from.</param>
        /// <param name="inlines">The <see cref="InlineCollection"/> of the parent element.</param>
        /// <returns>The <see cref="Inline"/> to be appended to the parent.</returns>
        protected virtual Inline GenerateInlineForNode(INode node, InlineCollection inlines)
        {
            switch (node.NodeName)
            {
                case "S":
                case "STRIKE":
                    return GenerateStrike(node);
                case "IMG":
                    return GenerateInlineImage(node as IHtmlImageElement);
                case "A":
                    return GenerateLink(node as IHtmlAnchorElement);
                case "STRONG":
                case "B":
                    return GenerateBold(node);
                case "I":
                case "EM":
                case "CITE":
                    return GenerateItalic(node);
                case "INS":
                case "U":
                    return GenerateUnderline(node);
                case "BR":
                    return GenerateLineBreak();
                case "HR":
                    return GenerateHorizontalRule();
                case "SPAN":
                    return GenerateSpan(node);
                case "CODE":
                    return GenerateCode(node);
                case "Q":
                    return GenerateQuote(node as IHtmlQuoteElement);
                case "ABBR":
                    return GenerateAbbreviation(node as IHtmlElement);
                case "MARK":
                    return GenerateMark(node);
                case "SMALL":
                    return GenerateSmall(node);
                // Technically `IFRAME` is allowed as an inline, but this is not common
                // so we have no default handling for it.
                case "#text":
                default:
                    if (node.HasChildNodes)
                    {
                        return GenerateSpan(node);
                    }
                    return GeneratePlainText(node);
            }
        }

        //Hongjia's code
        public delegate void OnHtmlConvertedHandler(RichTextBlock htmlXaml);
        public OnHtmlConvertedHandler OnHtmlConverted;

        /// <summary>
        /// Creates <see cref="UIElement"/> elements which can be added to a <see cref="UIElementCollection"/>.
        /// </summary>
        /// <param name="node">The <see cref="INode"/> to generate the <see cref="UIElement"/> from.</param>
        /// <param name="elements">The <see cref="UIElementCollection"/> of the parent element.</param>
        /// <returns>The <see cref="UIElement"/> to be appended to the parent.</returns>
        protected virtual Block GenerateBlockForNode(INode node, BlockCollection elements)
        {
            //Block lastBlock = null;
            switch (node.NodeName)
            {
                case "S":
                case "STRIKE":
                    var strike = GenerateStrike(node);
                    return AddInlineToTextBlock(elements, strike);
                case "LI": // Treat <li> outside of a <ul> or <ol> as regular Paragraph.
                case "P":
                    var paragraph = GenerateParagraph(node as IHtmlParagraphElement);
                    return paragraph;
                case "IMG":
                    Paragraph imgParagraph = new Paragraph();
                    var imgNode = (IHtmlImageElement)node;
                    if(imgNode.ClassName == "FirstInDiv" || imgNode.ClassName == "FirstAndLastInDiv")
                    {
                        startWithAnotherParagraph = true;
                    }
                    InlineUIContainer inlineUIContainer = new InlineUIContainer();
                    inlineUIContainer.Child = GenerateImage(node as IHtmlImageElement);
                    imgParagraph.Inlines.Add(inlineUIContainer);
                    return imgParagraph;
                case "A":
                    var link = GenerateLink(node as IHtmlAnchorElement);
                    bool treatAsInlineLink = false;
                    var previousNode = node.PreviousSibling;
                    if(previousNode is IHtmlParagraphElement)
                    {
                        if ((previousNode as IHtmlParagraphElement).HasAttribute("style")
                            && (previousNode as IHtmlParagraphElement).Attributes["style"].Value == "display: inline;")
                        {
                            treatAsInlineLink = true;
                        }
                        treatAsInlineLink = true;
                    }
                    if(treatAsInlineLink)
                    {
                        return AddInlineToTextBlock(elements, link, GetOrCreateLastParagraph(elements));
                    }
                    else
                    {
                        return AddInlineToTextBlock(elements, link);
                    }
                case "BLOCKQUOTE":
                    var blockquoteContainer = new InlineUIContainer()
                    {
                        Child = GenerateBlockQuote(node)
                    };
                    var blockquoteParagraph = new Paragraph();
                    blockquoteParagraph.Inlines.Add(blockquoteContainer);
                    return blockquoteParagraph;
                case "STRONG":
                case "B":
                    var bold = GenerateBold(node);
                    return AddInlineToTextBlock(elements, bold);
                case "I":
                case "EM":
                case "CITE":
                    var italic = GenerateItalic(node);
                    return AddInlineToTextBlock(elements, italic);
                case "INS":
                case "U":
                    var underline = GenerateUnderline(node);
                    return AddInlineToTextBlock(elements, underline);
                case "HR":
                    var rule = GenerateHorizontalRule();
                    return AddInlineToTextBlock(elements, rule, new Paragraph());
                case "BR":
                    var linebreak = GenerateLineBreak();
                    return AddInlineToTextBlock(elements, linebreak);
                case "SPAN":
                    var span = GenerateSpan(node);
                    return AddInlineToTextBlock(elements, span);
                case "IFRAME":
                    var iframeContainer = new InlineUIContainer()
                    {
                        Child = GenerateIframe(node as IHtmlInlineFrameElement)
                    };
                    var iframeParagraph = new Paragraph();
                    iframeParagraph.Inlines.Add(iframeContainer);
                    return iframeParagraph;
                case "H1":
                case "H2":
                case "H3":
                case "H4":
                case "H5":
                case "H6":
                    //lastBlock = GetOrCreateLastRichTextBlock(elements);
                    var headerParagraph = new Paragraph()
                    {
                        Margin = new Thickness(0, 19.5, 0, 3)
                    };
                    var header = GenerateHeader(node);
                    headerParagraph.Inlines.Add(header);
                    return headerParagraph;
                case "UL":
                    return GenerateUL(node as IHtmlUnorderedListElement);
                case "OL":
                    return GenerateOL(node as IHtmlOrderedListElement);
                case "CODE":
                    var code = GenerateCode(node);
                    return AddInlineToTextBlock(elements, code);
                case "PRE":
                    var preInlineContainer = new InlineUIContainer()
                    {
                        Child = GeneratePreformatted(node as IHtmlPreElement)
                    };
                    var preParagraph = new Paragraph();
                    preParagraph.Inlines.Add(preInlineContainer);
                    return preParagraph;
                case "Q":
                    var quote = GenerateQuote(node as IHtmlQuoteElement);
                    return AddInlineToTextBlock(elements, quote);
                case "ABBR":
                    var abbreviation = GenerateAbbreviation(node as IHtmlElement);
                    return AddInlineToTextBlock(elements, abbreviation);
                case "MARK":
                    var mark = GenerateMark(node);
                    return AddInlineToTextBlock(elements, mark);
                case "SMALL":
                    var small = GenerateSmall(node);
                    return AddInlineToTextBlock(elements, small);
                case "#text":
                default:
                    var plainText = GeneratePlainText(node);
                    return AddInlineToTextBlock(elements, plainText);
            }
        }


        //Hongjia's codes begin
        /// <summary>
        /// After the text being selected
        /// </summary>
        /// <remarks>Written by Hongjia</remarks>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LastTextBlock_SelectionChanged(object sender, RoutedEventArgs e)
        {
            SelectionChanged?.Invoke(sender, e);
        }

        //public delegate void SelectionChangedEventHandler(string selectedText);
        private event SelectionChangedEventHandler SelectionChanged;

        event Generators.SelectionChangedEventHandler IHtmlXamlGenerator.SelectionChanged
        {
            add
            {
                SelectionChanged += value;
            }

            remove
            {
                SelectionChanged -= value;
            }
        }

        //Hongjia's codes end

        /// <summary>
        /// Loops through children of an <see cref="INode"/> and appends as <see cref="UIElement"/> to given <see cref="UIElementCollection"/>.
        /// </summary>
        /// <param name="node">The parent <see cref="INode"/>.</param>
        /// <param name="elements">The <see cref="UIElementCollection"/> collection to add elements to.</param>
        protected void AddChildren(INode node, BlockCollection elements)
        {
            var nodeQueue = UnfoldTagToCollection(node);
            foreach (var child in nodeQueue)
            {
                var element = GenerateBlockForNode(child, elements);

                if (elements.LastOrDefault() != element)
                    elements.Add(element);
            }
        }

        /// <summary>
        /// 展开那些布局用的标签
        /// </summary>
        /// <param name="node"></param>
        protected Queue<INode> UnfoldTagToCollection(INode node)
        {
            Queue<INode> nodeQueue = new Queue<INode>();
            Stack<INode> nodeStack = new Stack<INode>();

            //Initialize queue
            var childNodesinReverseOrder = node.ChildNodes.Reverse();
            foreach (var childNode in childNodesinReverseOrder)
            {
                nodeStack.Push(childNode);
            }

            do
            {
                var topNode = nodeStack.Pop();
                if (IsCustomizeOrDivTag(topNode))
                {
                    //若是DIV的第一个，则必须要注释换行！
                    if (topNode.NodeName == "DIV")
                    {
                        if (topNode.ChildNodes.FirstOrDefault() is IHtmlElement)
                        {
                            //string attributeStyle = null;
                            IHtmlElement paragraphElement = (IHtmlElement)topNode.ChildNodes.FirstOrDefault();
                            paragraphElement.ClassName = "FirstInDiv";
                        }
                        if(topNode.ChildNodes.LastOrDefault() is IHtmlElement)
                        {
                            IHtmlElement paragraphElement = (IHtmlElement)topNode.ChildNodes.LastOrDefault();
                            if (paragraphElement.ClassName != "FirstInDiv")
                            {
                                paragraphElement.ClassName = "LastInDiv";
                            }
                            else
                            {
                                paragraphElement.ClassName = "FirstAndLastInDiv";
                            }
                        }
                    }

                    var reversedNodes = topNode.ChildNodes.Reverse();

                    foreach (var childNode in reversedNodes)
                    {
                        nodeStack.Push(childNode);
                    }
                }
                else
                {
                    if(nodeQueue.LastOrDefault() is IHtmlElement && topNode is IHtmlElement)
                    {
                        var preNode = nodeQueue.LastOrDefault() as IHtmlElement;
                        var htmlNode = topNode as IHtmlElement;
                        if(preNode.ClassName == "LastInDiv" || preNode.ClassName =="FirstAndLastInDiv")
                        {
                            if (htmlNode.ClassName == "LastInDiv" || htmlNode.ClassName == "FirstAndLastInDiv")
                            {
                                htmlNode.ClassName = "FirstAndLastInDiv";
                            }
                            else
                            {
                                htmlNode.ClassName = "FirstInDiv";
                            }
                        }
                    }

                    nodeQueue.Enqueue(topNode);
                }
            } while (nodeStack.Count != 0);

            return nodeQueue;
        }

        /// <summary>
        /// 检测节点是否是布局用的节点
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private bool IsCustomizeOrDivTag(INode node)
        {
            switch (node.NodeName)
            {
                case "S":
                case "STRIKE":
                case "LI": // Treat <li> outside of a <ul> or <ol> as regular Paragraph.
                case "P":
                case "IMG":
                case "A":
                case "BLOCKQUOTE":
                case "STRONG":
                case "B":
                case "I":
                case "EM":
                case "CITE":
                case "INS":
                case "U":
                case "HR":
                case "BR":
                case "SPAN":
                case "IFRAME":
                case "H1":
                case "H2":
                case "H3":
                case "H4":
                case "H5":
                case "H6":
                case "UL":
                case "OL":
                case "CODE":
                case "PRE":
                case "Q":
                case "ABBR":
                case "MARK":
                case "SMALL":
                case "#text":
                    return false;
                case "DIV":
                default:
                    return true;
            }
        }

        /// <summary>
        /// Loops through children of an <see cref="INode"/> and appends as <see cref="Inline"/> to given <see cref="InlineCollection"/>.
        /// </summary>
        /// <param name="node">The parent <see cref="INode"/>.</param>
        /// <param name="inlines">The <see cref="InlineCollection"/> collection to add inlines to.</param>
        protected void AddInlineChildren(INode node, InlineCollection inlines)
        {
            bool isInDiv = false;
            if(node.NodeName == "DIV")
            {
                isInDiv = true;
            }
            if(isInDiv)
            {
                inlines.Add(new LineBreak());
            }
            foreach (var child in node.ChildNodes)
            {
                var inline = GenerateInlineForNode(child, inlines);

                try
                {
                    if (inlines.LastOrDefault() != inline)
                        inlines.Add(inline);
                }
                catch(ArgumentException exception)
                {
                    System.Diagnostics.Debug.WriteLine(exception.Message);
                }

            }
            if(isInDiv)
            {
                inlines.Add(new LineBreak());
            }
        }

        /// <summary>
        /// Given a <see cref="BlockCollection"/>, appends the given <see cref="Inline"/> to a <see cref="Paragraph"/>.
        /// </summary>
        /// <param name="blocks">The <see cref="BlockCollection"/> collection to add elements to.</param>
        /// <param name="inline">The <see cref="Inline"/> to add to the collection.</param>
        /// <param name="paragraph">The <see cref="Paragraph"/> to add. Will get from collection or create one if none provided using <see cref="GetOrCreateLastParagraph(BlockCollection)"/>.</param>
        /// <returns></returns>
        protected Block AddInlineToTextBlock(BlockCollection blocks, Inline inline, Paragraph paragraph = null)
        {
            if(startWithAnotherParagraph && paragraph == null)
            {
                paragraph = new Paragraph();
                startWithAnotherParagraph = false;
            }
            else
            {
                paragraph = paragraph ?? GetOrCreateLastParagraph(blocks);
            }
            //paragraph = paragraph ?? new Paragraph();
            paragraph.Inlines.Add(inline);

            //if (blocks.LastOrDefault() != paragraph)
            //    blocks.Add(paragraph);

            return paragraph;
        }

        /// <summary>
        /// Gets a <see cref="RichTextBlock"/> from given <see cref="UIElementCollection"/> or creates and adds one.
        /// </summary>
        /// <param name="elements">The <see cref="UIElementCollection"/> collection to add <see cref="RichTextBlock"/> to.</param>
        /// <returns>The last <see cref="RichTextBlock"/> of the collection.</returns>
        protected RichTextBlock GetOrCreateLastRichTextBlock(UIElementCollection elements)
        {
            if (elements.LastOrDefault() is RichTextBlock textBlock)
                return textBlock;

            textBlock = new RichTextBlock();
            elements.Add(textBlock);

            return textBlock;
        }

        private string PrepareRawHtml(string rawHtml)
        {
            if (String.IsNullOrWhiteSpace(rawHtml))
                return "";

            var preTags = new Dictionary<string, string>();
            string processedHtml = rawHtml;

            // Saves our <pre> tags in dictionary so the whitespace removal doesn't
            // strip from these tags. They'll get added back after that step.
            var matches = _preTagRegex.Matches(processedHtml);
            int currentIndex = 0;
            foreach (Match match in matches)
            {
                var key = $"{Guid.NewGuid().ToString()}_{currentIndex}";
                processedHtml = processedHtml.Replace(match.Value, key);
                preTags.Add(key, match.Value);
                currentIndex++;
            }

            // Removes whitespace between tags, which HTML does not normally render.
            processedHtml = _htmlWhitespaceRegex.Replace(processedHtml, String.Empty);
            processedHtml = processedHtml.Trim();
            // For some reason the regex leaves the returns (\r). 
            // TODO: Figure out why and remove this extra step.
            processedHtml = processedHtml.Replace("\r", String.Empty);

            // Adds our saved <pre> tags back to the HTML with whitespace preserved.
            foreach (var preMatch in preTags)
            {
                processedHtml = processedHtml.Replace(preMatch.Key, preMatch.Value);
            }

            return processedHtml;
        }

        private Paragraph GetOrCreateLastParagraph(BlockCollection blocks)
        {
            var lastBlock = blocks.LastOrDefault();
            if (lastBlock != null)
                return (Paragraph)lastBlock;

            var paragraph = new Paragraph();
            blocks.Add(paragraph);

            return paragraph;
        }

        //private StackPanel GenerateDiv(INode node)
        //{
        //    var stackPanel = new StackPanel();

        //    AddChildren(node, stackPanel.Children);

        //    return stackPanel;
        //}
        private bool startWithAnotherParagraph = false;
        private Paragraph GenerateParagraph(IHtmlParagraphElement node)
        {
            Paragraph paragraph = null;
            string attributeStyle = null;
            if (node.ClassName!="FirstInDiv" && node.ClassName != "FirstAndLastInDiv" && node.HasAttribute("style"))
            {
                attributeStyle = node.Attributes["style"].Value;
            }
            if (attributeStyle == "display: inline;")
            {
                paragraph = GetOrCreateLastParagraph(richTextBlock.Blocks);
                AddInlineChildren(node, paragraph.Inlines);
            }
            else
            {
                paragraph = new Paragraph();
                AddInlineChildren(node, paragraph.Inlines);
            }
            if (attributeStyle != "display: inline;")
            {
                startWithAnotherParagraph = true;
            }
            return paragraph;
        }

        private Block GenerateUL(IHtmlUnorderedListElement node)
        {
            var ulParagraph = new Paragraph()
            {
                LineHeight = 3,
                Margin = new Thickness(0, 3, 0, 3)
            };

            foreach (var child in node.ChildNodes)
            {
                if (String.IsNullOrWhiteSpace(child.TextContent))
                    continue;

                var liInlineContainer = new InlineUIContainer();
                var dot = new TextBlock() { Text = "•", Margin = new Thickness(9.5, 0, 9.5, 0) };
                liInlineContainer.Child = dot;
                ulParagraph.Inlines.Add(liInlineContainer); //添加"•"

                //var horizontalStackPanel = new StackPanel() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 0, 3) };
                //horizontalStackPanel.Children.Add(new TextBlock() { Text = "•", Margin = new Thickness(9.5, 0, 9.5, 0) });

                //var richTextBlock = new RichTextBlock();

                AddInlineChildren(child, ulParagraph.Inlines);

                //richTextBlock.Blocks.Add(paragraph);
                //horizontalStackPanel.Children.Add(richTextBlock);
                //ulParagraph.Children.Add(horizontalStackPanel);
                ulParagraph.Inlines.Add(new LineBreak());
            }
            ulParagraph.Inlines.RemoveAt(ulParagraph.Inlines.Count - 1); //Delete Last LineBreak (Because this is a block, means next content will start in another line anyway, so one more LineBreak makes one more line spacing.)
            return ulParagraph;
        }

        private Block GenerateOL(IHtmlOrderedListElement node)
        {
            var olParagraph = new Paragraph();

            int number = node.Start;
            foreach (var child in node.ChildNodes)
            {
                if (String.IsNullOrWhiteSpace(child.TextContent))
                    continue;

                var liInlineContainer = new InlineUIContainer();
                var dot = new TextBlock() { Text = $"{number}.", Margin = new Thickness(9.5, 0, 9.5, 0) };
                liInlineContainer.Child = dot;
                olParagraph.Inlines.Add(liInlineContainer); //添加"•"
                ++number;

                AddInlineChildren(child, olParagraph.Inlines);

                olParagraph.Inlines.Add(new LineBreak());
            }

            return olParagraph;
        }

        private FrameworkElement GenerateImage(IHtmlImageElement node)
        {
            var image = new Image()
            {
                HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Center
            };

            var viewBox = new Viewbox()
            {
                Stretch = Stretch.UniformToFill,
                StretchDirection = StretchDirection.DownOnly,
                HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Center
            };

             viewBox.Child = image;

            if (Uri.TryCreate(node.Source, UriKind.RelativeOrAbsolute, out Uri src))
            {
                var bitmap = new BitmapImage(src);
                image.Source = bitmap;
            }

            return viewBox;
        }

        private Inline GenerateInlineImage(IHtmlImageElement node)
        {
            var inlineContainer = new InlineUIContainer();

            var image = GenerateImage(node);

            inlineContainer.Child = image;

            return inlineContainer;
        }

        private bool isLinkGenerating = false;
        private Inline GenerateLink(IHtmlAnchorElement node)
        {
            isLinkGenerating = true;
            bool hasImg = false;
            Uri imageUrl = null;
            GoDeep(node, ref hasImg, ref imageUrl);
            if (hasImg)
            {
                var hyperlinkContainer = new InlineUIContainer();
                var hyperlinkButton = new HyperlinkButton();

                var bitmap = new BitmapImage(imageUrl);
                Image image = new Image
                {
                    Source = bitmap
                };
                var viewBox = new Viewbox()
                {
                    Stretch = Stretch.UniformToFill,
                    StretchDirection = StretchDirection.DownOnly,
                    Child = image
                };
                if (Uri.TryCreate(node.Href, UriKind.RelativeOrAbsolute, out Uri hrefUri))
                {
                    try
                    {
                        hyperlinkButton.NavigateUri = hrefUri;
                    }
                    catch (NullReferenceException)
                    { }
                }

                hyperlinkContainer.Child = hyperlinkButton;
                hyperlinkButton.Content = viewBox;

                return hyperlinkContainer;
            }
            else
            {
                var hyperlink = new Hyperlink();

                if (Uri.TryCreate(node.Href, UriKind.RelativeOrAbsolute, out Uri hrefUri))
                {
                    try
                    {
                        hyperlink.NavigateUri = hrefUri;
                    }
                    catch(System.NullReferenceException)
                    {
                        System.Diagnostics.Debug.WriteLine("超链接错误");
                    }
                }

                // TODO: Add option for unfurling links as images
                // TODO: Add link clicked event

                AddInlineChildren(node, hyperlink.Inlines);
                return hyperlink;
            }


        }

        private void GoDeep(IElement node, ref bool hasImg, ref Uri imageUrl)
        {
            if (node == null)
            {
                return;
            }
            foreach (var childNode in node.Children)
            {
                if (childNode.NodeName == "IMG")
                {
                    hasImg = true;
                    var imageSourceUrl = childNode.Attributes["src"].Value;
                    //IHtmlImageElement imgNode = childNode as IHtmlImageElement;
                    Uri.TryCreate(imageSourceUrl, UriKind.RelativeOrAbsolute, out Uri hrefUri);
                    imageUrl = hrefUri;
                    return;
                }
                if (!hasImg)
                {
                    GoDeep(childNode, ref hasImg, ref imageUrl);
                }
            }
        }

        private Inline GenerateBold(INode node)
        {
            var bold = new Bold();

            AddInlineChildren(node, bold.Inlines);

            return bold;
        }

        private Inline GenerateItalic(INode node)
        {
            var italic = new Italic();

            AddInlineChildren(node, italic.Inlines);

            return italic;
        }

        private Inline GenerateUnderline(INode node)
        {
            var underline = new Underline();

            AddInlineChildren(node, underline.Inlines);

            return underline;
        }

        private Inline GenerateStrike(INode node)
        {
            var span = new Span()
            {
                TextDecorations = TextDecorations.Strikethrough,
            };

            AddInlineChildren(node, span.Inlines);

            return span;
        }

        private Inline GenerateLineBreak()
        {
            return new LineBreak();
        }

        private Inline GenerateSpan(INode node)
        {
            var span = new Span();

            AddInlineChildren(node, span.Inlines);

            return span;
        }

        private UIElement GeneratePreformatted(IHtmlPreElement node)
        {
            var border = new Border()
            {
                Style = PreformattedBorderStyle,
            };

            if (node.FirstChild.NodeName == "CODE")
            {
                var codeBlock = GeneratePreformattedCodeBlock(node.FirstChild as IHtmlElement);
                border.Child = codeBlock;
            }
            else
            {
                var span = new Span()
                {
                    FontFamily = new FontFamily("Consolas"),
                };

                var run = new Run()
                {
                    Text = node.TextContent,
                };

                span.Inlines.Add(run);

                var textBlock = new RichTextBlock();
                var paragraph = new Paragraph();

                paragraph.Inlines.Add(span);
                textBlock.Blocks.Add(paragraph);

                border.Child = textBlock;
            }

            return border;
        }

        private UIElement GeneratePreformattedCodeBlock(IHtmlElement node)
        {
            string language = node.GetAttribute("class");
            HighlightLanguage highlightLanguage = HighlightLanguage.PlainText;
            switch (language)
            {
                case "python":
                    highlightLanguage = HighlightLanguage.Python;
                    break;
                case "javascript":
                case "js":
                case "jsx":
                    highlightLanguage = HighlightLanguage.JavaScript;
                    break;
                case "json":
                    highlightLanguage = HighlightLanguage.JSON;
                    break;
                case "cs":
                case "csharp":
                    highlightLanguage = HighlightLanguage.CSharp;
                    break;
                case "c":
                case "c++":
                case "cc":
                case "cpp":
                    highlightLanguage = HighlightLanguage.CPlusPlus;
                    break;
                case "css":
                    highlightLanguage = HighlightLanguage.CSS;
                    break;
                case "php":
                    highlightLanguage = HighlightLanguage.PHP;
                    break;
                case "ruby":
                case "rb":
                    highlightLanguage = HighlightLanguage.Ruby;
                    break;
                case "html":
                case "xml":
                case "xhtml":
                case "rss":
                    highlightLanguage = HighlightLanguage.XML;
                    break;
                case "java":
                case "jsp":
                    highlightLanguage = HighlightLanguage.Java;
                    break;
                case "sql":
                    highlightLanguage = HighlightLanguage.SQL;
                    break;
                default:
                    break;
            }

            return new CodeHighlightedTextBlock()
            {
                Code = CleanText(node.InnerHtml),
                HighlightLanguage = highlightLanguage,
            };
        }

        private Inline GenerateCode(INode node)
        {
            var span = new Span()
            {
                FontFamily = new FontFamily("Consolas"),
                Foreground = new SolidColorBrush(Colors.Red),
            };

            var run = new Run()
            {
                Text = node.TextContent,
            };

            span.Inlines.Add(run);

            return span;
        }

        private Inline GenerateHeader(INode node)
        {
            var span = new Span();

            AddInlineChildren(node, span.Inlines);

            switch (node.NodeName)
            {
                case "H1":
                    span.FontSize = 32;
                    break;
                case "H2":
                    span.FontSize = 28;
                    break;
                case "H3":
                    span.FontSize = 24;
                    break;
                case "H4":
                    span.FontSize = 20;
                    break;
                case "H5":
                    span.FontSize = 18;
                    break;
                case "H6":
                    span.FontSize = 14;
                    break;
                default:
                    span.FontSize = 14;
                    break;
            }
            span.FontWeight = FontWeights.SemiBold;

            return span;
        }

        private Inline GenerateSmall(INode node)
        {
            var span = new Span()
            {
                FontSize = 12,
            };

            AddInlineChildren(node, span.Inlines);

            return span;
        }

        private Inline GenerateMark(INode node)
        {
            var span = new Span()
            {
                TextDecorations = TextDecorations.Underline,
                Foreground = new SolidColorBrush(Colors.Goldenrod),
                FontWeight = FontWeights.SemiBold,
            };

            AddInlineChildren(node, span.Inlines);

            return span;
        }

        private Inline GenerateAbbreviation(IHtmlElement node)
        {
            var span = new Span()
            {
                TextDecorations = TextDecorations.Underline,
            };

            AddInlineChildren(node, span.Inlines);

            if (node.HasAttribute("title"))
            {
                ToolTip toolTip = new ToolTip()
                {
                    Content = node.GetAttribute("title"),
                };

                ToolTipService.SetToolTip(span, toolTip);
            }

            return span;
        }

        private UIElement GenerateBlockQuote(INode node)
        {
            RichTextBlock blockquoteRichTextBlock = new RichTextBlock();
            AddChildren(node, blockquoteRichTextBlock.Blocks);
            var border = new Border()
            {
                Style = BlockquoteBorderStyle,
                Child = blockquoteRichTextBlock
            };

            return border;
        }

        private Inline GenerateQuote(INode node)
        {
            var run = new Run()
            {
                Text = $"“{node.TextContent}”",
                FontStyle = FontStyle.Italic,
            };

            return run;
        }

        private Inline GenerateHorizontalRule()
        {
            var inlineContainer = new InlineUIContainer();
            var line = new Line
            {
                X1 = 0,
                Y1 = 0,
                X2 = 1000,
                Y2 = 0,
                Stroke = new SolidColorBrush(Colors.Gray),
                StrokeThickness = 1,
                Margin = new Thickness(0, 9.5, 0, 9.5),
            };
            inlineContainer.Child = line;

            return inlineContainer;
        }

        private UIElement GenerateIframe(IHtmlInlineFrameElement node)
        {
            var webView = new WebView(WebViewExecutionMode.SeparateThread);

            if (!String.IsNullOrEmpty(node.ContentHtml))
            {
                webView.NavigateToString(node.ContentHtml);
            }
            else if (Uri.TryCreate(node.Source, UriKind.RelativeOrAbsolute, out Uri sourceUrl))
            {
                webView.Navigate(sourceUrl);
            }

            return webView;
        }

        private Inline GeneratePlainText(INode node)
        {
            var textRun = GenerateTextRun(node.TextContent);

            return textRun;
        }

        private Run GenerateTextRun(string textContent)
        {
            return new Run()
            {
                Text = CleanText(textContent),
            };
        }

        private string CleanText(string input)
        {
            if (String.IsNullOrWhiteSpace(input))
                return String.Empty;

            string clean = WebUtility.HtmlDecode(input);
            if (clean == "\0")
                clean = "\n";

            return clean;
        }
    }
}
