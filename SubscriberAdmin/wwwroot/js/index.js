if (!localStorage.getItem('token')) window.location.href = 'login.html';

let logout = document.getElementById('logout');
logout.addEventListener('click', () => {
    let id = localStorage.getItem('id');

    fetch(`https://test.genesys-tech.com/subs/${id}`, {
        method: 'DELETE',
        headers: {
            'Content-Type': 'application/json; charset=utf-8'
        },
        cache: 'no-cache',
        body: ''
    }).then(r => {
        console.log(r);
        localStorage.clear();
        window.location.href = 'login.html';
    });
});

let subscribe = document.getElementById('subscribe');
subscribe.addEventListener('click', () => {
    let id = localStorage.getItem('id');

    if (id != '') {
        window.location.href = `https://notify-bot.line.me/oauth/authorize?response_type=code&client_id=FdFAtkbLdfWWrGNznSjPnQ&redirect_uri=https%3A%2F%2Ftest.genesys-tech.com%2Fcallbacknotify&scope=notify&state=${id}`;
    } else {
        localStorage.clear();
        window.location.href = 'login.html';
    }
});

let sMessage = document.getElementById('send-message');
sMessage.addEventListener('click', () => {
    let message = document.getElementById('message').value;
    if (message != '') {
        let id = localStorage.getItem('id');
        console.log(message.value);
        if (id == '') {
            localStorage.clear();
            window.location.href = 'login.html';
        } else {
            fetch(`https://test.genesys-tech.com/notify`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json; charset=utf-8'
                },
                cache: 'no-cache',
                body: JSON.stringify({
                    id: id,
                    message: message
                })
            }).then(r => {
                if (r.ok & r.status == 200) alert('發送訊息成功。');
                else alert(`status=${r.status}`)
            });
        }
    } else alert('請輸入訊息。');
});

let unsubscribe = document.getElementById('unsubscribe');
unsubscribe.addEventListener('click', () => {
    let id = localStorage.getItem('id');

    fetch(`https://test.genesys-tech.com/notify/${id}`, {
        method: 'PUT',
        headers: {
            'Content-Type': 'application/json; charset=utf-8'
        },
        cache: 'no-cache',
        body: ''
    }).then(r => {
        console.log(r);
        if (r.ok & r.status == 200) alert('取消訂閱成功。');
        else alert(`status=${r.status}`)
    });
});