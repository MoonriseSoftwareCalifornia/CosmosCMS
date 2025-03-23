/**
 * @file Duplicator.js
 * @summary A JavaScript module that provides a duplicator widget.
 * @version 1.0.0
 * @since 1.0.0
 * @module Duplicator
 * @description This module provides a duplicator widget that allows users to duplicate elements on a page.
 */
const Duplicator = (function () {

    /**
     * Creates a clone node or element.
     * @memberof Duplicator
     * @returns a clone node or element.
     * @type {HTMLElement}
     */
    function createClone(element) {
        let clone = null;

        if (typeof this.component !== "undefined"
            && this.component !== null
            && this.component !== ""
            && typeof this.component.get !== "undefined"
            && typeof this.component.get === "function") {
            clone = this.component.get();
        } else {
            clone = element.cloneNode(true);
        }

        const container = clone.querySelector('.icon-container');
        if (container) {
            container.remove();
        }
        return clone;
    }

    /**
     * Handles the mouse over event.
     * @param {any} event
     * @memberof Duplicator
     * @returns
     */
    function mouseOverHandler(event) {
        let element = event.currentTarget;
        let iconContainer = element.querySelector('.icon-container');
        if (iconContainer !== null) {
            return;
        }

        iconContainer = document.createElement('div');
        iconContainer.className = 'icon-container';

        iconContainer.style.position = 'absolute';
        iconContainer.style.top = '5%';
        iconContainer.style.left = '50%';
        iconContainer.style.transform = "translate(-50%, -50%)";
        iconContainer.style.zIndex = window.getComputedStyle(element).zIndex + 10;

        const moveUp = document.createElement('button');
        moveUp.className = 'icon-button';
        moveUp.innerHTML = '<i class="fa-solid fa-arrow-up"></i>'; // You can replace this with any icon
        moveUp.title = 'Move this item up.';

        moveUp.onclick = () => {
            if (element.previousElementSibling) {
                element.parentNode.insertBefore(element, element.previousElementSibling);
                savePageBody();
            } else {
                alert("Already at top.");
            }
        };
        
        iconContainer.appendChild(moveUp);

        const above = document.createElement("button");
        above.className = 'icon-button';
        above.innerHTML = '<i class="fa-solid fa-file-arrow-up"></i>'; // You can replace this with any icon
        above.title = 'Clone and place above.';
        above.onclick = () => {
            const clone = createClone(element);
            element.parentNode.insertBefore(clone, element);
            onCreate(clone);
            create(clone, this.onCreate, this.onDelete, this.component);
            // Code to save the page body.
            // Found on /Views/Editor/Edit.cshtml
            savePageBody();
        };
        iconContainer.appendChild(above);

        const del = document.createElement("button");
        del.className = 'icon-button';
        del.style.marginLeft = "5px";
        del.style.marginRight = "5px";
        del.innerHTML = '<i class="fa-solid fa-trash-can"></i>'; // You can replace this with any icon
        del.title = 'Permanently remove this item.';
        del.onclick = () => {
            if (confirm("Are you sure you want to delete this item?")) {
                var children = element.parentElement.querySelectorAll(".ccms-clone");
                if (children.length === 1) {
                    alert("You cannot delete the last item.");
                    return;
                }
                onDelete(element);
                element.remove();
                // Code to save the page body.
                // Found on /Views/Editor/Edit.cshtml
                savePageBody();
            } else {
                // Code to cancel the deletion
                console.log("Deletion canceled.");
            }
        };

        iconContainer.appendChild(del);

        const below = document.createElement('button');
        below.className = 'icon-button';
        below.innerHTML = '<i class="fa-solid fa-file-arrow-down"></i>'; // You can replace this with any icon
        below.title = 'Clone and place below.';
        below.onclick = () => {
            const clone = createClone(element);
            element.insertAdjacentElement('afterend', clone);
            onCreate(clone);
            create(clone, this.onCreate, this.onDelete, this.component);
            // Code to save the page body.
            // Found on /Views/Editor/Edit.cshtml
            savePageBody();
        };
        iconContainer.appendChild(below);

        const moveDown = document.createElement('button');
        moveDown.className = 'icon-button';
        moveDown.innerHTML = '<i class="fa-solid fa-arrow-down"></i>'; // You can replace this with any icon
        moveDown.title = 'Move this item down.';
        moveDown.onclick = () => {
            if (element.nextElementSibling) {
                element.parentNode.insertBefore(element.nextElementSibling, element);
                savePageBody();
            } else {
                alert("Already at bottom.");
            }
        };
        iconContainer.appendChild(moveDown);

        element.appendChild(iconContainer);

        element.addEventListener('mouseleave', mouseLeaveHandler);
        element.removeEventListener('mouseover', mouseOverHandler);
    }

    /**
     * Creates a supported editor for the specified element created by the duplicator.
     * @param {any} item
     * @description Normally editors are created when the page is loaded. This function is used to create editors for elements that are created dynamically.
     */
    function onCreate(item) {
        const childDivs = item.querySelectorAll(`div[data-ccms-ceid]`);
        childDivs.forEach(function (element) {
            element.setAttribute('data-ccms-ceid', ccms__generateGUID());
            const type = element.getAttribute("data-editor-config");
            if (type && type === "image-widget") {
                ccms___setupImageWidget(element);
            } else {
                createCkEditor(element);
            }
        });
    }

    /**
     * Destroys the editor for the specified element.
     * @param {any} item
     */
    function onDelete(item) {

        // Destroy the editor for the specified element.
        const childDivs = item.querySelectorAll(`div[data-ccms-ceid]`);
        childDivs.forEach(function (element) {
            if (typeof element.ckeditorInstance !== "undefined" && element.ckeditorInstance !== null) {
                element.ckeditorInstance.destroy();
            } else {
                const pond = FilePond.find(element);
                if (typeof pond !== "undefined" && pond !== null) {
                    pond.destroy();
                }
                var trashcans = element.querySelectorAll('.ccms-img-trashcan');
                trashcans.forEach(trashcan => {
                    trashcan.remove();
                });
            }
            element.removeAttribute('data-tippy-content');
            element.removeAttribute('aria-label');
        });
    }

    /**
     * Saves the page body.
     * @param {any} element
     * @returns
     * @memberof Duplicator
     */
    function savePageBody() {

        // Get all elements that need an editor to be closed prior to saving.
        const items = document.querySelectorAll('.ccms-clone');

        // Close all editors.
        items.forEach(item => {
            onDelete(item);
        });

        parent.savePageBody(document.querySelector('div[ccms-content-id]').innerHTML);

        // Reopen all editors.
        items.forEach(editor => {
            onCreate(editor);
        });
    }

    /**
     * Handles the mouse leave event.
     * @param {any} event
     * @returns
     * @memberof Duplicator
     */
    function mouseLeaveHandler(event) {
        let element = event.currentTarget;
        const iconContainer = element.querySelector('.icon-container');

        if (iconContainer !== null) {
            iconContainer.remove();
        }

        element.removeEventListener('mouseleave', mouseLeaveHandler);
        element.removeEventListener('mouseover', mouseOverHandler);
        element.addEventListener('mouseover', mouseOverHandler, { once: true });
    }

    return {
        /**
         * Creates a duplicator widget.
         * @param {any} element - The HTML element to duplicate
         * @memberof Duplicator
         * @returns - A duplicator widget configured with the specified element.
         * @example
         * Duplicator.create(element);
         */
        create: function (target) {
            if (!target) return;

            target.style.position = "relative"; // Required for tool bar placement

            let iconContainer = target.querySelector('.icon-container');
            if (iconContainer !== null) {
                iconContainer.remove();
            }

            if (!target.classList.contains('ccms-clone')) {
                target.classList.add('ccms-clone');
            };

            target.addEventListener('mouseover', mouseOverHandler, { once: true });
        }
    }

})();