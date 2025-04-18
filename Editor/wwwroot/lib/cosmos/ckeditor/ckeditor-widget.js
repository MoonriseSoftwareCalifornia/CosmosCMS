// CKEditor configuration for Cosmos CMS.
import {
    InlineEditor,
    Autoformat,
    AutoImage,
    Autosave,
    BalloonToolbar,
    BlockQuote,
    Bold,
    CodeBlock,
    Essentials,
    Heading,
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
    Paragraph,
    PasteFromOffice,
    SimpleUploadAdapter,
    Table,
    TableCaption,
    TableCellProperties,
    TableColumnResize,
    TableProperties,
    TableToolbar,
    TextTransformation,
    TodoList,
    Underline
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
    plugins: [
        Autoformat,
        AutoImage,
        Autosave,
        BalloonToolbar,
        BlockQuote,
        Bold,
        CodeBlock,
        Essentials,
        Heading,
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
        Paragraph,
        PasteFromOffice,
        SimpleUploadAdapter,
        Table,
        TableCaption,
        TableCellProperties,
        TableColumnResize,
        TableProperties,
        TableToolbar,
        TextTransformation,
        TodoList,
        Underline,

        FileLink,
        InsertImage,
        PageLinkUI,
        VsCodeEditor,
        SignalR,
    ],
    balloonToolbar: ['bold', 'italic', 'underline', '|', 'link', 'pageLink', 'insertImage', '|', 'bulletedList', 'numberedList'],
    placeholder: 'Add your content here.',
    licenseKey: LICENSE_KEY,
    toolbar: {
        items: [
            'heading',
            '|',
            'imageInsert',
            'insertImage',
            'mediaEmbed',
            'insertTable',
            'blockQuote',
            'codeBlock',
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
        waitingTime: 1000, // in ms
        save(editor) {
            if (parent.enableAutoSave === true) {
                return parent.saveEditorRegion(editor.getData(), editor.sourceElement.getAttribute("data-ccms-ceid"));
            }
        }
    },
    simpleUpload: {
        // The URL that the images are uploaded to.
        uploadUrl: '/FileManager/SimpleUpload/' + articleNumber,
            // Enable the XMLHttpRequest.withCredentials property.
            withCredentials: true
        },
    menuBar: {
        isVisible: false
    },
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
        decorators: {
            openInNewTab: {
                mode: 'manual',
                label: 'Open in a new tab',
                attributes: {
                    target: '_blank',
                    rel: 'noopener noreferrer'
                }
            }
        },
        defaultProtocol: 'https://',
        decorators: {
            toggleDownloadable: {
                mode: 'manual',
                label: 'Downloadable',
                attributes: {
                    download: 'file'
                }
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
    ui: {
        viewportOffset: {
            top: getDistanceFromTop(),
        }
    },
    table: {
        contentToolbar: ['tableColumn', 'tableRow', 'mergeTableCells', 'tableProperties', 'tableCellProperties']
    }
};

function ccms___createEditor(editorElement) {

    if (typeof editorElement.ckeditorInstance !== "undefined" && editorElement.ckeditorInstance !== null) {
        return; //
    }

    const isNew = editorElement.getAttribute("data-ccms-new");

    if (isNew) {
        const guid = ccms__generateGUID(); // This function is defined in the Editor/wwwroot/lib/cosmos/dublicator/dublicator.js file.
        editorElement.setAttribute("data-ccms-ceid", guid);
        editorElement.removeAttribute("data-ccms-new");
    }

    InlineEditor
        .create(editorElement, EditorConfig)
        .then(editor => {
            window.editor = editor;
            const imageUploadEditing = editor.plugins.get('ImageUploadEditing');
            imageUploadEditing.on('uploadComplete', (evt, { data, imageElement }) => {
                parent.ccms_setBannerImage(data.url);
            });
            editor.editing.view.document.on('change:isFocused', (evt, data, isFocused) => {
                console.log(`View document is focused: ${isFocused}.`);
                if (isFocused) {
                    focusedEditor = editor;
                } else {
                    focusedEditor = null;
                }
            });
            ccms_editors.push(editor);
        });
}

function ccms___createEditors() {
    // Editor instances
    const editorElements = document.querySelectorAll('[data-ccms-ceid]');
    editorElements.forEach(editorElement => {
        // let config;
        let editorType = 'default';

        if (editorElement.hasAttribute('data-editor-config')) {
            editorType = editorElement.getAttribute('data-editor-config').toLowerCase();
        }

        if (editorType !== 'image-widget') {
            ccms___createEditor(editorElement); // Function in ckeditor-widget.js
        }
    });
}

window.createCkEditor = ccms___createEditor;

document.addEventListener("DOMContentLoaded", function (event) {
    ccms___createEditors();
});