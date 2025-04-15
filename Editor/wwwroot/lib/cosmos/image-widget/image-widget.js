function ccms___onClickTrashCan(id, element) {
    // Clean things up.
    while (element.hasChildNodes()) {
        element.removeChild(element.firstChild);
    }
    parent.saveChanges(element.innerHTML, id);

    ccms___initializePond(element);
}

function newGuid() {
    function s4() {
        return Math.floor((1 + Math.random()) * 0x10000)
            .toString(16)
            .substring(1);
    }
    return s4() + s4() + '-' + s4() + '-' + s4() + '-' + s4() + '-' + s4() + s4() + s4();
}

function debounce(func, wait) {
    let timeout;
    return function (...args) {
        clearTimeout(timeout);
        timeout = setTimeout(() => func.apply(this, args), wait);
    };
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

function ccms___handleWidgetMouseLeave(event) {

    // Ensure image element exists.
    const element = event.currentTarget;
    const trashCan = element.querySelector(".ccms-img-trashcan");

    // Check if the mouse is moving to the trashCan or its children
    if (trashCan && trashCan === event.relatedTarget) {
        return;
    }

    if (trashCan) {
        trashCan.remove();
    }

    element.removeEventListener('mouseleave', ccms___handleWidgetMouseLeave);
    element.removeEventListener('mouseover', ccms___handleWidgetMouseOver);
    element.addEventListener('mouseover', ccms___handleWidgetMouseOver, { once: true });
    console.log("Mouse over event reset.");
}

function ccms___handleWidgetMouseOver(event) {
    // Ensure image element exists.
    const img = this.querySelector("img");
    const element = event.currentTarget;
    if (img !== null && img.src !== "") {

        // If an image already present add the remove image option.
        const id = element.getAttribute("data-ccms-ceid");
        let a = element.querySelector(".ccms-img-trashcan");

        if (a !== null) {
            return;
        }

        a = document.createElement("button");

        a.id = "ccms___trash-" + id;
        a.classList.add("ccms-img-trashcan");
        a.innerHTML = '<i class="fa-solid fa-trash"></i>';
        a.title = "Click to remove image.";
        a.style.position = "absolute";
        a.style.top = "50%";
        a.style.left = "50%";
        a.style.transform = "translate(-50%, -50%)";
        a.style.zIndex = window.getComputedStyle(element).zIndex + 10;

        a.onclick = function (e) {
            e.preventDefault();
            ccms___onClickTrashCan(id, element);
        }
        element.appendChild(a);

        console.log("Mouse over image widget. Added trash can.");
    }
    console.log("Mouse over image widget. Removed mouse over event.");
    console.log("Mouse out event reset.");
    element.addEventListener('mouseleave', ccms___handleWidgetMouseLeave);
    element.removeEventListener('mouseover', ccms___handleWidgetMouseOver);
}

function ccms___setupImageWidget(element) {

    // Setup the style for the image container.
    element.style.position = "relative";
    element.display = "inline-block";

    const isNew = element.getAttribute("data-ccms-new");
    // Clear out placeholder image.
    const placeHolder = element.querySelector(".ccms___placeHolder");
    if (placeHolder) { placeHolder.remove(); }

    // Clear out filepond drop area if it exists.
    const ponds = element.querySelectorAll(".filepond--root");
    ponds.forEach(pond => { pond.remove(); });

    let id = element.getAttribute("data-ccms-ceid");

    if (typeof id === "undefined" && isNew === "undefined") {
        return;
    }

    if (isNew) {
        const guid = newGuid(); // This function is defined in the Editor/wwwroot/lib/cosmos/dublicator/dublicator.js file.
        element.setAttribute("data-ccms-ceid", guid);
        element.removeAttribute("data-ccms-new");
        id = element.getAttribute("data-ccms-ceid");
    }

    const img = element.querySelector("img");
    if (img !== null) {
        element.addEventListener('mouseleave', ccms___handleWidgetMouseLeave);
        element.addEventListener('mouseover', ccms___handleWidgetMouseOver, { once: true });
        console.log("Mouse events added.");
    } else {
        element.childNodes.forEach(node => {
            node.remove();
        });
        element.removeEventListener('mouseleave', ccms___handleWidgetMouseLeave);
        element.removeEventListener('mouseover', ccms___handleWidgetMouseOver);
        ccms___initializePond(element);
    }
}

function ccms___initializePond(element) {

    const id = element.getAttribute("data-ccms-ceid");

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
    pond.editorId = id;
    pond.inputElement = input;

    pond.setOptions({
        server: "/FileManager/UploadImage"
    });

    pond.on('addfile', (error, file) => {
        const savingMsg = document.createElement("span");
        savingMsg.innerHTML = "Uploading image ... ";
        savingMsg.id = "saving-" + pond.editorId;

        if (typeof parent.saving !== "undefined") {
            parent.saving();
            parent.saveInProgress = true;
        }

        if (articleNumber) {
            file.setMetadata("Path", "/pub/articles/" + articleNumber + "/");
        } else {
            file.setMetadata("Path", "/pub/images/");
        }
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
        //const relativePath = "/pub/articles/" + articleNumber + "/" + fileName;
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
        image.classList.add("ccms-img-widget-img");

        element.appendChild(image);

        if (typeof parent.saveEditorRegion == "function") {
            const id = element.getAttribute("data-ccms-ceid");
            parent.saveEditorRegion(element.innerHTML, id);
        }

        ccms___setupImageWidget(element);
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

    if (!element.hasChildNodes()) {
        const img = document.createElement('img');
        img.classList.add("ccms___placeHolder");
        img.style.display = "block";
        img.style.margin = "auto";
        img.style.height = "60px";
        img.src = "/images/AddImageHere.webp";
        element.appendChild(img);
    }
}

document.addEventListener('DOMContentLoaded', function () {

    FilePond.registerPlugin(
        FilePondPluginFileMetadata,
    );

    const imageContainers = document.querySelectorAll('div[data-editor-config="image-widget"]');
    imageContainers.forEach(ccms___setupImageWidget);
});
