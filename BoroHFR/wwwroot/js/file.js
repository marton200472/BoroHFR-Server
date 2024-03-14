$("#fileInput").change(() => uploadFiles());

function AddFiles() {
    $("#fileInput").trigger('click');
}

async function removeFile(e) {
    var tr = e.parentElement.parentElement;
    var guid = tr.getElementsByTagName("input")[0].value;
    await fetch("/api/file/" + guid, { method: "DELETE" });
    tr.remove();
}

function uploadFiles() {
    var fileInput = $("#fileInput")[0];
    var files = fileInput.files;
    for (var i = 0; i < files.length; i++) {
        uploadFile(files[i]);
    }
}

function uploadFile(file) {
    var formData = new FormData();
    formData.append('file', file);

    var progressBarContainer = document.createElement('div'); // Container for progress bar and file name
    progressBarContainer.className = 'progress-container';

    var fileName = document.createElement('div'); // Display file name
    fileName.className = 'file-name';
    fileName.textContent = file.name;

    var progressBar = document.createElement('div'); // Create a new progress bar element
    progressBar.className = 'progress-bar';

    progressBarContainer.appendChild(progressBar);

    var progressBarsContainer = document.getElementById("uploadProgressTable").getElementsByTagName("tbody")[0];

    var newRow = document.createElement('tr'); // Create a new table row
    var newCell = document.createElement('td'); // Create a new table cell
    var newCell2 = document.createElement('td'); // Create a new table cell
    var newCell3 = document.createElement('td');
    newCell3.style.width = "50px";
    newCell.appendChild(fileName);
    newCell2.appendChild(progressBarContainer);

    var delButton = document.createElement('button');
    delButton.type = "button";
    delButton.addEventListener("click", () => removeFile(delButton));
    delButton.className = "btn-close btn-close-white";
    newCell3.appendChild(delButton);

    newRow.appendChild(newCell);
    newRow.appendChild(newCell2);
    newRow.appendChild(newCell3);
    progressBarsContainer.appendChild(newRow);

    var xhr = new XMLHttpRequest();

    xhr.upload.addEventListener('progress', function (event) {
        if (event.lengthComputable) {
            var percent = Math.round((event.loaded / event.total) * 100);
            progressBar.style.width = percent + '%';
            progressBar.innerHTML = percent + '%';
        }
    });

    xhr.addEventListener('load', function (event) {
        var files = JSON.parse(event.target.responseText);
        for (var i = 0; i < files.length; i++) {
            var field = document.createElement("input");
            field.setAttribute("type", "hidden");
            field.setAttribute("name", "attachments");
            field.value = files[i];
            $(newRow).append(field);
        }
        $("#fileInput")[0].value = '';
    });

    xhr.open('POST', '/api/file', true);
    xhr.send(formData);
}