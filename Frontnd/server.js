const express = require('express');
const cors = require('cors');
const mysql = require('mysql2/promise');
const jwt = require('jsonwebtoken');

const app = express();
app.use(express.json());
app.use(cors());

const db = mysql.createPool({
    host: '127.0.0.1',
    user: 'JohnServer',
    password: 'Jhony2007',
    database: 'project'
});

const SECRET_KEY = '63b0acb4f4b38cd21b8cf43c6a5ea6f5137a5b5ad9e1c55de5426c0da20319461ba6860a027bd109633d24e950ae68a8637fa12d1f28d602c5bffa7f589b1b1ce87ffb8cf663541dd61004b16bfdd7fcabc5a0dd5fdbc67c3b3c392cf8859cd8458a00789710731b2602535f55c141bfcf2b3dde23803415c8e5dcc9b5dda88710ced1d488611a1d58e6f6f4de7840d9a710c1094426f31dceddc63175c40050a44485accaebdc9622c298d3ac6db8342b94a67967f2a3ef0aefcb2bffe969a9b969ebb8a0d1f4fc515a7b91f4b350f74ae8cb36433e419c7cd1eb902d7362fd5b7e1baf59c683269d6ed9efe1ca9f3d99aba59af4e4421d92fd02c42d964b30'; // מה?

async function authenticateToken(req, res, next) {
    const token = req.headers.authorization?.split(' ')[1];
    if (!token) return res.sendStatus(401);

    try {
        const [rows] = await db.query(`SELECT user_id FROM tokens WHERE token = ?`, [token]);
        
        if (rows.length === 0) {
            console.log("Didn't found a token on the table.");
            return res.sendStatus(403);
        }

        req.user = { id: rows[0].user_id };
        console.log("Authentication passed , found the user ", req.user.id);
        next();
    } catch (error) {
        console.error("An error while trying to get the token: ", error);
        res.status(500).json({ message: "Server error" });
    }
}

// התחברות משתמש
// app.post('/signin', async (req, res) => {
//     const { email, password } = req.body;
//     try {
//         const [rows] = await db.query(`SELECT * FROM users WHERE email ='${email}' AND password ='${password}' `);
//         if (rows.length > 0) {
//             const user = rows[0];
//             const token = jwt.sign({ id: user.id }, SECRET_KEY, { expiresIn: '1h' });
//             res.json({ message: "Login successful", token });
//         } else {
//             res.status(401).json({ message: "Invalid credentials" });
//         }
//     } catch (error) {
//         res.status(500).json({ message: "Server error" });
//     }
// });

app.post('/signup', async (req, res) => {
    const { username, email, password } = req.body;

    try {
        // Check if the email already exists
        const [existingUser] = await db.query(`SELECT * FROM users WHERE email = ?`, [email]);
        if (existingUser.length > 0) {
            return res.status(400).json({ message: "Email already registered" });
        }

        // Insert the new user
        await db.query(`INSERT INTO users (username, email, password) VALUES (?, ?, ?)`, [username, email, password]);

        res.json({ message: "User registered successfully" });
    } catch (error) {
        console.error("Error during signup:", error);
        res.status(500).json({ message: "Server error" });
    }
});



app.post('/signin', async (req, res) => {
    const { email, password } = req.body;
    try {
        // Check if user exists
        const [rows] = await db.query(`SELECT * FROM users WHERE email = ? AND password = ?`, [email, password]);

        if (rows.length > 0) {
            const user = rows[0];
            const token = jwt.sign({ id: user.id }, SECRET_KEY, { expiresIn: '1h' });

            // Delete old tokens for this user
            await db.query(`DELETE FROM tokens WHERE user_id = ?`, [user.id]);

            // Insert the new token
            await db.query(`INSERT INTO tokens (user_id, token) VALUES (?, ?)`, [user.id, token]);

            res.json({ message: "Login successful", token });
        } else {
            res.status(401).json({ message: "Invalid credentials" });
        }
    } catch (error) {
        console.error("Error during signin:", error);
        res.status(500).json({ message: "Server error" });
    }
});




app.post('/lockDevice', authenticateToken, async (req, res) => {
    const userId = req.user.id;
    console.log(`Got a request to lock userId: ${userId}`); // Debugging

    try {
        // בדיקה אם המשתמש קיים
        const [userExists] = await db.query(`SELECT * FROM users WHERE id = ?`, [userId]);
        if (userExists.length === 0) {
            return res.status(404).json({ message: "User not found" });
        }

        // עדכון נעילה
        const [result] = await db.query('UPDATE users SET should_lock = 1 WHERE id = ?', [userId]);

        if (result.affectedRows > 0) {
            console.log(`User : ${userId} account locked successfully`);
            res.json({ message: "User account locked successfully" });
        } else {
            console.log(`User: ${userId} not found or already locked`);
            res.status(404).json({ message: "User not found or already locked" });
        }
    } catch (error) {
        console.error("Error updating user status", error);
        res.status(500).json({ message: "Error updating user status" });
    }
});

app.listen(3000, () => console.log('Server running on port 3000'));

