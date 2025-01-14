document.getElementById('fileInput').addEventListener('change', function (event) {
    const file = event.target.files[0];
    if (file) {
        const reader = new FileReader();
        reader.onload = function (e) {
            const base64String = e.target.result;
            const img = document.createElement('img');
            img.src = base64String;
            document.getElementById('imageContainer').appendChild(img);
        };
        reader.readAsDataURL(file);
    }
});