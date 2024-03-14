const chatRoot = document.getElementById("chatRoot");
var userId;
var openChannels = new Set();

async function GetUserId() {
    var response = await fetch("/api/userdata/userid");
    var text = await response.text();
    return text;
}

function IsLocalUser(id) {
    return id === userId;
}

const chat = new function () {
    

    this.connection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/chat")
        .configureLogging(signalR.LogLevel.Information)
        .withAutomaticReconnect()
        .build();

    

    this.Setup=async function() {
        userId = await GetUserId();
        this.connection.on("ReceiveMessage", this.ReceiveMessage);
    }

    this.ReceiveMessage = function(channelId, senderId, senderName, msg, msgId, sendTime, attachments) {
        if (!openChannels.has(channelId)) {
            this.connection.send("UnsubscribeFromConversation", channelId);
            return;
        }
        var msgP = document.createElement("p");
        msgP.innerText = msg;
        msgP.id = "msg-" + msgId;
        for (var i = 0; i < attachments.length; i++) {
            var a = document.createElement("a");
            a.href = "/api/file/" + attachments[i].id.value;
            a.innerText = attachments[i].downloadName;
            msgP.innerHTML += "<br>";
            msgP.appendChild(a);
        }
        var box = $("#chat-box-" + channelId + " .ps-container");
        var lastSection = $(".media-chat:last .media-body", box);

        var shouldScrollToBottom = box[0].scrollTop + box[0].clientHeight == box[0].scrollHeight;

        if (lastSection !== undefined && $(".meta .chat-username", lastSection).attr("uid") == senderId && (Date.parse(sendTime) - Date.parse($(".meta time", lastSection).attr("datetime"))) / 60000 < 1) {
            $(".meta", lastSection).before(msgP);
        }
        else {
            var section = document.createElement("div");
            section.className = "media media-chat" + (IsLocalUser(senderId) ? " media-chat-reverse" : "");
            var mbody = document.createElement("div");
            mbody.className = "media-body";
            mbody.appendChild(msgP);
            var timeLabel = document.createElement("p");
            timeLabel.className = "meta";
            var usrSpan = document.createElement("span");
            usrSpan.className = "chat-username";
            usrSpan.innerText = senderName;
            usrSpan.setAttribute("uid", senderId);
            var tm = document.createElement("time");
            var d = new Date(Date.parse(sendTime));
            tm.dateTime = d.toISOString();
            var minutes = d.getMinutes();
            tm.innerText = d.getHours() + ":" + (minutes < 10 ? '0' + minutes : minutes);
            timeLabel.appendChild(usrSpan);
            timeLabel.innerHTML += " - ";
            timeLabel.appendChild(tm);
            mbody.appendChild(timeLabel);
            section.appendChild(mbody);
            box.append(section);
        }

        if (shouldScrollToBottom) {
            box[0].scrollTo(0, box[0].scrollHeight);
        }
    }

    this.uploadFiles = function(channelId) {
        var fileInput = $("#chat-box-" + channelId + " input[type=file]")[0];
        var files = fileInput.files;
        //var allowedExtensions = ['.jpg', '.jpeg', '.png', '.pdf', '.svg', '.zip', '.docx', '.xlsx'];
        for (var i = 0; i < files.length; i++) {
            //var fileExtension = files[i].name.substring(files[i].name.lastIndexOf('.')).toLowerCase();
            this.uploadFile(files[i], channelId);
            /*if (allowedExtensions.includes(fileExtension)) {
                uploadFile(files[i]);
            } else {
                alert('Invalid file type: ' + fileExtension);
            }*/
        }
    }

    this.uploadFile = function(file, channelId) {
        var formData = new FormData();
        formData.append('file', file);

        var progressBarContainer = document.createElement('div'); // Container for progress bar and file name
        progressBarContainer.className = 'progress-container';

        var fileName = document.createElement('div'); // Display file name
        fileName.className = 'file-name';
        fileName.textContent = file.name;

        var progressBar = document.createElement('div'); // Create a new progress bar element
        progressBar.className = 'progress-bar';
        progressBar.id = 'progressBar_' + file.name;

        progressBarContainer.appendChild(progressBar);

        var progressBarsContainer = document.getElementById("chat-box-" + channelId).getElementsByClassName("ath_container")[0].getElementsByTagName("table")[0];;

        var newRow = document.createElement('tr'); // Create a new table row
        var newCell = document.createElement('td'); // Create a new table cell
        var newCell2 = document.createElement('td'); // Create a new table cell
        newCell.appendChild(fileName);
        newCell2.appendChild(progressBarContainer);
        newRow.appendChild(newCell);
        newRow.appendChild(newCell2);
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
            // var uploadStatus = document.getElementById('uploadStatus');
            // uploadStatus.innerHTML = event.target.responseText;
            // Reset the input field of type "file"
            var files = JSON.parse(event.target.responseText);
            for (var i = 0; i < files.length; i++) {
                var field = document.createElement("input");
                field.setAttribute("type", "hidden");
                field.setAttribute("name", "attachment");
                field.value = files[i];
                $("#chat-box-" + channelId + " .publisher .attachment-guids").append(field);
            }
            $("#chat-box-" + channelId + " input[type=file]")[0].value = '';
        });

        xhr.open('POST', '/api/file', true);
        xhr.send(formData);
    }

    this.startClient = async function() {
        while (this.connection.state === signalR.HubConnectionState.Disconnected) {
            try {
                await this.connection.start();
                console.log("SignalR Connected.");
                return;

            } catch (err) {
                console.log(err);
                await new Promise(r => setTimeout(r, 5000));
            }
        }

    }

    this.toggleMinimize = function(channelId) {
        $("#chat-box-" + channelId).toggleClass("chat-minimized");
    }

    this.OpenChannel = async function(channelId, minimized = false) {
        if (this.connection.state === signalR.HubConnectionState.Disconnected) {
            await this.startClient();
        }

        if (openChannels.has(channelId)) {
            return;
        }
        openChannels.add(channelId);
        var chatBox = document.createElement("div");
        chatBox.className = "chat-box chat-card card card-bordered bg-dark text-light" + (minimized ? " chat-minimized" : "");
        chatBox.id = "chat-box-" + channelId;
        
        var resp = await fetch("/chat/" + channelId);
        if (!resp.ok) {
            alert("Couldn't open chat. (Are you a member?)");
            return;
        }
        chatBox.innerHTML = await resp.text();
        await this.connection.invoke("SubscribeToConversation", channelId);

        $(".publisher", chatBox).submit(async ev => {
            ev.preventDefault();
            var message = $(".publisher .publisher-input", chatBox).val().trim();
            if (message.length == 0) {
                return;
            }
            var attachments = [];
            var att = $(".publisher .attachment-guids input");
            for (var i = 0; i < att.length; i++) {
                attachments.push(att[i].value);
            }
            await this.connection.invoke("SendMessage", channelId, message, attachments);
            $(".ath_container table", chatBox).empty();
            $(".publisher .attachment-guids", chatBox).empty();
            $(".publisher input[type=text]", chatBox).val("");
        });
        $(".card-header .btn-close", chatBox).click(() => this.CloseChannel(channelId));


        $(".upload-button", chatBox).click(ev => {
            ev.preventDefault();
            $("input[type=file]", ev.currentTarget.parentElement.parentElement).trigger('click');
        });

        $("input[type=file]", chatBox).change(() => this.uploadFiles(channelId));

        chatRoot.appendChild(chatBox);
        var scroller = chatBox.getElementsByClassName("ps-container")[0];
        scroller.scrollTo(0, scroller.scrollHeight);
    }

    this.CloseChannel = function(channelId) {
        if (!openChannels.has(channelId)) {
            return;
        }
        openChannels.delete(channelId);
        $("#chat-box-" + channelId).remove();
        if (openChannels.size == 0) {
            this.connection.stop();
        }
        else {
            this.connection.send("UnsubscribeFromConversation", channelId);
        }
    }
}





chat.Setup();























