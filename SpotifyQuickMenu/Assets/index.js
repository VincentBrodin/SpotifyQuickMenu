window.chrome.webview.addEventListener('message', (event) => {
    const id = event.data.id;
    const content = JSON.parse(event.data.content);
    switch (id) {
        case "track":
            OnTrackUpdate(content);
            break;
        case "set-volume":
            SetVolume(content);
            break;
    }
});

let currentTrack;
let draggingTimestamp = false;

function OnTrackUpdate(track) {
    const play_icon = document.getElementById("play-icon");
    if (track.is_playing) {
        play_icon.classList.remove("bi-play-fill");
        play_icon.classList.add("bi-pause-fill");
    }
    else {
        play_icon.classList.add("bi-play-fill");
        play_icon.classList.remove("bi-pause-fill");
    }

    const track_img = document.getElementById("track-img");
    track_img.src = track.item.album.images[0].url;

    const track_name = document.getElementById("track-name");
    track_name.innerText = track.item.name;

    const track_album = document.getElementById("track-album");
    track_album.innerText = track.item.album.name;


    let artists = track.item.artists[0].name;
    const artistsLen = track.item.artists.length;
    for (let i = 1; i < artistsLen - 1; i++) {
        artists += `, ${track.item.artists[i].name}`;
    }
    if (track.item.artists.length > 1) {
        artists += ` and ${track.item.artists[artistsLen - 1].name}`;
    }

    const track_artists = document.getElementById("track-artists");
    track_artists.innerText = artists;

    const end_timestamp = document.getElementById("end-timestamp");
    end_timestamp.innerText = MsToString(track.item.duration_ms);


    if (!draggingTimestamp) {
        const current_timestamp = document.getElementById("current-timestamp");
        current_timestamp.innerText = MsToString(track.progress_ms);

        const timestamp = document.getElementById("timestamp");
        timestamp.value = (track.progress_ms / track.item.duration_ms) * 100;
    }

    currentTrack = track;

    StartScrollIfOverflow();
}

function SetVolume(content) {
    const volume = document.getElementById("volume");
    volume.value = content;
}


function OnVolumeChange() {
    const volume = document.getElementById("volume");
    const volume_icon = document.getElementById("volume-icon");
    const value = volume.value;

    if (value == 0) {
        volume_icon.classList.add("bi-volume-mute");
        volume_icon.classList.remove("bi-volume-down");
        volume_icon.classList.remove("bi-volume-up");
    }
    else if (value < 50) {
        volume_icon.classList.remove("bi-volume-mute");
        volume_icon.classList.add("bi-volume-down");
        volume_icon.classList.remove("bi-volume-up");
    }
    else {
        volume_icon.classList.remove("bi-volume-mute");
        volume_icon.classList.remove("bi-volume-down");
        volume_icon.classList.add("bi-volume-up");
    }

    //Sending data
    const data = {
        "id": "volume",
        "content": value,
    }
    window.chrome.webview.postMessage(data);
}

function OnTimestampUpdate() {
    if (currentTrack == null) {
        return;
    }
    const timestamp = document.getElementById("timestamp");
    const current_timestamp = document.getElementById("current-timestamp");
    current_timestamp.innerText = MsToString((timestamp.value / 100) * currentTrack.item.duration_ms);
}

function OnTimestampChange() {
    if (currentTrack == null) {
        return;
    }
    const timestamp = document.getElementById("timestamp");
    const time_ms = (timestamp.value / 100) * currentTrack.item.duration_ms;


    const json = {
        "context_uri": currentTrack.item.album.uri,
        "position": currentTrack.item.track_number - 1,
        "position_ms": Math.round(time_ms),
    }

    console.log(currentTrack);

    const data = {
        "id": "set_time",
        "content": JSON.stringify(json),
    }
    console.table(data);
    window.chrome.webview.postMessage(data);
}


function MouseDownTimestamp() {
    draggingTimestamp = true;
}

function MouseUpTimestamp() {
    draggingTimestamp = false;
}

function Next() {
    const data = {
        "id": "next",
    }
    window.chrome.webview.postMessage(data);
}

function Previous() {
    const data = {
        "id": "previous",
    }
    window.chrome.webview.postMessage(data);
}

function Play() {
    if (currentTrack.is_playing) {
        const data = {
            "id": "pause",
        }

        window.chrome.webview.postMessage(data);
    }
    else {
        const data = {
            "id": "play",
        }

        window.chrome.webview.postMessage(data);
    }
}

function MsToString(ms) {
    const time = ms / 1000.0;
    const mind = time % (60 * 60);
    const minutes = String(Math.floor(mind / 60)).padStart(2, '0');

    const secd = mind % 60;
    const seconds = String(Math.ceil(secd)).padStart(2, '0');

    return `${minutes}:${seconds}`;
}

function StartScrollIfOverflow() {
    const scrollTexts = document.querySelectorAll('.media-text');

    scrollTexts.forEach(scrollText => {
        const container = scrollText.parentElement;
        const containerWidth = container.offsetWidth;
        const textWidth = scrollText.scrollWidth;

        if (textWidth > containerWidth) {
            const scrollDuration = textWidth / 50;
            scrollText.style.animation = `scroll ${scrollDuration}s linear infinite`;
        } else {
            scrollText.style.animation = 'none';
        }
    });
}