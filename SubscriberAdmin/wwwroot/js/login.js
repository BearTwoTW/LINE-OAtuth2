let token = '';
if (window.location.search != '') {
    let parameterStr = window.location.search.split('?')[1];
    let parameterArr = parameterStr.split('&');
    parameterArr.forEach(item => {
        let key = item.split('=')[0];
        let value = item.split('=')[1];
        if (value != '') {
            localStorage.setItem(key, value);
        }
        window.location.href = 'index.html';
    });
}

if (localStorage.getItem('token')) window.location.href = 'index.html';

let login = document.getElementById('login');
login.addEventListener('click', () => {
    alert('這是假的。')
});