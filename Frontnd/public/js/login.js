document.getElementById("signupForm").addEventListener("submit", event => {
    event.preventDefault();

    const username = document.getElementById("username").value;
    const email = document.getElementById("email").value;
    const password = document.getElementById("password").value;

    fetch('http://localhost:3000/signup', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ username, email, password })
    })
    .then(response => response.json())
    .then(data => {
        alert(data.message);
    })
    .catch(err => console.error(err));
});

document.getElementById("signinForm").addEventListener("submit", event => {
    event.preventDefault();

    const email = document.getElementById("email2").value;
    const password = document.getElementById("password2").value;

    fetch('http://localhost:3000/signin', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password })
    })
    .then(response => response.json())
    .then(data => {
        console.log(data);  // להדפיס את התגובה של השרת
        if (data.token) {
            localStorage.setItem("token", data.token);
            window.location.href = "lock_or_switch.html"; // מעבר לעמוד הבא
        } else {
            alert("Invalid credentials");
        }
    })
    .catch(err => console.error(err));
});



const container = document.getElementById('container');
const registerBtn = document.getElementById('register');
const loginBtn = document.getElementById('login');

registerBtn.addEventListener('click', () => {
    container.classList.add("active");
});

loginBtn.addEventListener('click', () => {
    container.classList.remove("active");
});
document.getElementById("signinForm").addEventListener("submit", event => {
    event.preventDefault();

    const email = document.getElementById("email2").value;
    const password = document.getElementById("password2").value;

    fetch('http://localhost:3000/signin', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password })
    })
    .then(response => response.json())
    .then(data => {
        if (data.token) {
            localStorage.setItem("token", data.token);
            window.location.href = "lock_or_switch.html"; // מעבר לדף הנעילה או החלפת המשתמש
        } else {
            alert("Invalid credentials");
        }
    })
    .catch(err => console.error(err));
});
