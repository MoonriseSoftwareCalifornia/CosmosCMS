// CKEditor configuration for Cosmos CMS.
import {
    InlineEditor,
    Alignment,
    Autoformat,
    AutoImage,
    Autosave,
    BlockQuote,
    Bold,
    Bookmark,
    Code,
    CodeBlock,
    Essentials,
    FindAndReplace,
    FontBackgroundColor,
    FontColor,
    FontFamily,
    FontSize,
    GeneralHtmlSupport,
    Heading,
    Highlight,
    HorizontalLine,
    HtmlComment,
    HtmlEmbed,
    ImageBlock,
    ImageCaption,
    ImageInline,
    ImageInsert,
    ImageInsertViaUrl,
    ImageResize,
    ImageStyle,
    ImageTextAlternative,
    ImageToolbar,
    ImageUpload,
    Indent,
    IndentBlock,
    Italic,
    Link,
    LinkImage,
    List,
    ListProperties,
    MediaEmbed,
    Mention,
    Paragraph,
    PasteFromOffice,
    RemoveFormat,
    ShowBlocks,
    SimpleUploadAdapter,
    SpecialCharacters,
    SpecialCharactersArrows,
    SpecialCharactersCurrency,
    SpecialCharactersEssentials,
    SpecialCharactersLatin,
    SpecialCharactersMathematical,
    SpecialCharactersText,
    Strikethrough,
    Style,
    Subscript,
    Superscript,
    Table,
    TableCaption,
    TableCellProperties,
    TableColumnResize,
    TableProperties,
    TableToolbar,
    TextTransformation,
    Title,
    TodoList,
    Underline,
    WordCount
} from 'ckeditor5';

import FileLink from "filelink";
import InsertImage from "insertimage";
import PageLinkUI from "pagelink";
import VsCodeEditor from "vscodeeditor";
import SignalR from "signalr";

function getDistanceFromTop() {
    const element = document.getElementById('ccms---header---end');
    let distance = 0;
    while (element) {
        distance += element.offsetTop;
        element = element.offsetParent;
    }
    return distance;
}

/**
 * Create a free account with a trial: https://portal.ckeditor.com/checkout?plan=free
 */
const LICENSE_KEY = 'GPL'; // or <YOUR_LICENSE_KEY>.

const EditorConfig = {
    toolbar: {
        items: [

            'link',
            'pageLink',
            'fileLink',
            'insertImageViaUrl',
            'insertImage',
            'mediaEmbed',
            '|',
            'showBlocks',
            'findAndReplace',
            '|',
            'heading',
            'style',
            '|',
            'fontSize',
            'fontFamily',
            'fontColor',
            'fontBackgroundColor',
            '|',
            'bold',
            'italic',
            'underline',
            'strikethrough',
            'subscript',
            'superscript',
            'code',
            'removeFormat',
            '|',
            'specialCharacters',
            'horizontalLine',
            'bookmark',
            'insertTable',
            'highlight',
            'blockQuote',
            'codeBlock',
            'htmlEmbed',
            '|',
            'alignment',
            '|',
            'bulletedList',
            'numberedList',
            'todoList',
            'outdent',
            'indent'
        ],
        shouldNotGroupWhenFull: false
    },
    autosave: {
        waitingTime: 2800, // in ms
        save(editor) {
            if (parent.enableAutoSave === true) {
                return parent.cosmosSignalOthers(editor, "save");
            }
        }
    },
    menuBar: {
        isVisible: true
    },
    plugins: [
        Alignment,
        Autoformat,
        AutoImage,
        Autosave,
        BlockQuote,
        Bold,
        Bookmark,
        Code,
        CodeBlock,
        Essentials,
        FindAndReplace,
        FontBackgroundColor,
        FontColor,
        FontFamily,
        FontSize,
        GeneralHtmlSupport,
        Heading,
        Highlight,
        HorizontalLine,
        HtmlComment,
        HtmlEmbed,
        ImageBlock,
        ImageCaption,
        ImageInline,
        ImageInsert,
        ImageInsertViaUrl,
        ImageResize,
        ImageStyle,
        ImageTextAlternative,
        ImageToolbar,
        ImageUpload,
        Indent,
        IndentBlock,
        Italic,
        Link,
        LinkImage,
        List,
        ListProperties,
        MediaEmbed,
        Mention,
        Paragraph,
        PasteFromOffice,
        RemoveFormat,
        ShowBlocks,
        SimpleUploadAdapter,
        SpecialCharacters,
        SpecialCharactersArrows,
        SpecialCharactersCurrency,
        SpecialCharactersEssentials,
        SpecialCharactersLatin,
        SpecialCharactersMathematical,
        SpecialCharactersText,
        Strikethrough,
        Style,
        Subscript,
        Superscript,
        Table,
        TableCaption,
        TableCellProperties,
        TableColumnResize,
        TableProperties,
        TableToolbar,
        TextTransformation,
        Title,
        TodoList,
        Underline,
        WordCount,

        FileLink,
        InsertImage,
        PageLinkUI,
        VsCodeEditor,
        SignalR,
    ],
    fontFamily: {
        supportAllValues: true
    },
    fontSize: {
        options: [10, 12, 14, 'default', 18, 20, 22],
        supportAllValues: true
    },
    heading: {
        options: [
            {
                model: 'paragraph',
                title: 'Paragraph',
            },
            {
                model: 'heading1',
                view: 'h1',
                title: 'Heading 1',
            },
            {
                model: 'heading2',
                view: 'h2',
                title: 'Heading 2',
            },
            {
                model: 'heading3',
                view: 'h3',
                title: 'Heading 3',
            },
            {
                model: 'heading4',
                view: 'h4',
                title: 'Heading 4',
            },
            {
                model: 'heading5',
                view: 'h5',
                title: 'Heading 5',
            },
            {
                model: 'heading6',
                view: 'h6',
                title: 'Heading 6',
            }
        ]
    },
    htmlSupport: {
        allow: [
            {
                name: /^.*$/,
                styles: true,
                attributes: true,
                classes: true
            }
        ]
    },
    image: {
        toolbar: [
            'toggleImageCaption',
            'imageTextAlternative',
            '|',
            'imageStyle:inline',
            'imageStyle:wrapText',
            'imageStyle:breakText',
            '|',
            'resizeImage'
        ]
    },
    licenseKey: LICENSE_KEY,
    link: {
        addTargetToExternalLinks: true,
        defaultProtocol: 'https://',
        decorators: {
            toggleDownloadable: {
                mode: 'manual',
                label: 'Downloadable',
                attributes: {
                    download: 'file'
                }
            },
            openInNewTab: {
                mode: 'manual',
                label: 'Open in same tab',
                defaultValue: '_self',
                attributes: {
                    target: '_self'
                }
            }
        },
        list: {
            properties: {
                styles: true,
                startIndex: true,
                reversed: true
            }
        },
        mention: {
            feeds: [
                {
                    marker: '@Html.Raw("@")', // Excaped at symbol (Razor).
                    feed: [
                        /* See: https://ckeditor.com/docs/ckeditor5/latest/features/mentions.html */
                    ]
                }
            ]
        },
        placeholder: 'Type or paste your content here!',
        style: {
            definitions: [
                {
                    name: 'Article category',
                    element: 'h3',
                    classes: ['category']
                },
                {
                    name: 'Title',
                    element: 'h2',
                    classes: ['document-title']
                },
                {
                    name: 'Subtitle',
                    element: 'h3',
                    classes: ['document-subtitle']
                },
                {
                    name: 'Info box',
                    element: 'p',
                    classes: ['info-box']
                },
                {
                    name: 'Side quote',
                    element: 'blockquote',
                    classes: ['side-quote']
                },
                {
                    name: 'Marker',
                    element: 'span',
                    classes: ['marker']
                },
                {
                    name: 'Spoiler',
                    element: 'span',
                    classes: ['spoiler']
                },
                {
                    name: 'Code (dark)',
                    element: 'pre',
                    classes: ['fancy-code', 'fancy-code-dark']
                },
                {
                    name: 'Code (bright)',
                    element: 'pre',
                    classes: ['fancy-code', 'fancy-code-bright']
                }
            ]
        },
        ui: {
            viewportOffset: {
                top: getDistanceFromTop(),
            }
        },
        table: {
            contentToolbar: ['tableColumn', 'tableRow', 'mergeTableCells', 'tableProperties', 'tableCellProperties']
        },
        simpleUpload: {
            // The URL that the images are uploaded to.
            uploadUrl: '/FileManager/SimpleUpload/' + articleNumber,
            // Enable the XMLHttpRequest.withCredentials property.
            withCredentials: true
        },
    }
};

function ccms_createEditors() {
    // Editor instances
    const editorElements = document.querySelectorAll('[data-ccms-ceid]');
    editorElements.forEach(editorElement => {
        InlineEditor
            .create(editorElement, EditorConfig)
            .then(editor => {
                const imageUploadEditing = editor.plugins.get('ImageUploadEditing');
                imageUploadEditing.on('uploadComplete', (evt, { data, imageElement }) => {
                    parent.ccms_setBannerImage(data.url);
                });
                window.editor = editor;

                editor.editing.view.document.on('change:isFocused', (evt, data, isFocused) => {
                    console.log(`View document is focused: ${isFocused}.`);
                    if (isFocused) {
                        focusedEditor = editor;
                        parent.cosmosSignalOthers(editor, "join");
                    } else {
                        focusedEditor = null;
                    }
                });

                ccms_editors.push(editor);
            });
    });
}

function setMinimumMarginBottom() {

}

document.addEventListener("DOMContentLoaded", function (event) {
    ccms_createEditors();
});