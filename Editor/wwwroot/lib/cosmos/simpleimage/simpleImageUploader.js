function ccms___hooverDeleteButton(id, element) {
    // Clean things up.
    while (element.hasChildNodes()) {
        element.removeChild(element.firstChild);
    }
    parent.simpleImageEditorSave(element.innerHTML, id);
    ccms___setupPond(element);
}

function ccms___getImageDimensions(blob, callback) {
    const reader = new FileReader();
    reader.onload = function (e) {
        const img = new Image();
        img.onload = function () {
            const dimensions = {
                width: img.naturalWidth,
                height: img.naturalHeight
            };
            callback(dimensions);
        };
        img.src = e.target.result;
    };
    reader.readAsDataURL(blob);
}

function ccms___setupPond(element) {
    const id = element.getAttribute("data-ccms-ceid");

    if (typeof id === "undefined") {
        return;
    }

    // Ensure image element exists.
    let img = document.getElementById("img-" + id);

    if (typeof img !== "undefined" && img !== null && img.src !== "") {
        // If an image already present add the remove image option.
        const a = document.createElement("a");
        a.id = "ccms___trash-" + id;
        a.classList.add("text-overlay");
        a.innerHTML = '<i class="fa-solid fa-trash"></i>';
        a.title = "Click to remove image.";
        a.onclick = function (e) {
            e.preventDefault();
            ccms___hooverDeleteButton(id, element);
        }
        element.appendChild(a);
        return;
    }

    // '<input type="file" id="inp-' + id + '" class="filepond" name="files" accept="image/*" />'
    input = document.createElement("input");
    input.type = "file";
    input.id = "inp-" + id;
    input.classList.add("filepond");
    input.name = "files";
    element.appendChild(input);

    const pond = FilePond.create(input,
        {
            acceptedFileTypes: ['image/png', 'image/jpg', 'image/jpeg', 'image/webp', 'image/gif'],
            labelIdle: '<span class="filepond--label-action">upload image</span>',
            allowDrop: false
        });

    pond.editorElement = element;
    pond.imageElement = img;
    pond.editorId = id;
    pond.inputElement = input;

    pond.setOptions({
        server: "/FileManager/UploadImage"
    });

    pond.on('addfile', (error, file) => {
        const savingMsg = document.createElement("span");
        savingMsg.innerHTML = "Uploading image ... ";
        savingMsg.id = "saving-" + pond.editorId;
        parent.saving();
        parent.saveInProgress = true;
        file.setMetadata("Path", "/pub/articles/" + articleNumber + "/");
        file.setMetadata("RelativePath", "");
        file.setMetadata("fileName", file.filename.toLowerCase());
        ccms___getImageDimensions(file.file, function (dimensions) {
            file.setMetadata('imageWidth', dimensions.width);
            file.setMetadata('imageHeight', dimensions.height);
        });
    });

    pond.on('processfilestart', (e) => {
        const metadata = e.getMetadata();
    });

    pond.on('processfile', (error, file) => {
        const fileName = file.getMetadata("fileName");
        const relativePath = "/pub/articles/" + articleNumber + "/" + fileName;
        const element = pond.editorElement;
        const id = pond.editorId;

        // Clean things up.
        ccms___removePond(pond.inputElement.id)
        while (element.hasChildNodes()) {
            element.removeChild(element.firstChild);
        }

        const image = document.createElement("img");
        image.id = "img-" + id;
        image.src = file.serverId.replace(/['"]+/g, '');
        element.appendChild(image);

        parent.simpleImageEditorSave(element.innerHTML, id);
        ccms___setupPond(element);
        parent.doneSaving();
        parent.saveInProgress = false;
    });

    pond.on('removefile', (file) => {
        const f = file;
    });
}

function ccms___removePond(id) {
    // Clean things up.
    const element = document.getElementById(id);
    const pond = FilePond.find(element);
    element.remove();
    pond.destroy();
}

document.addEventListener('DOMContentLoaded', function () {

    FilePond.registerPlugin(
        FilePondPluginFileMetadata,
    );

    const imageContainers = document.querySelectorAll('.image-container');
    imageContainers.forEach(ccms___setupPond);
});