using System;
using System.Drawing;
using System.Windows.Forms;
using DotNetEnv;
using ScholarwaveCBTApp.UI;

namespace ScholarwaveCBTApp.Forms
{
    /// <summary>
    /// Login form for teacher authentication
    /// </summary>
    public class LoginForm : Form
    {
        private TextBox txtPassword;
        private MaterialButton btnLogin;
        
        public LoginForm()
        {
            InitializeComponents();
            AttachEventHandlers();
        }
        
        /// <summary>
        /// Initializes form controls
        /// </summary>
        private void InitializeComponents()
        {
            // Form properties
            Text = "Teacher Login";
            Width = 420;
            Height = 280;
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = true;
            MinimumSize = new Size(400, 260);
            ThemeManager.ApplyFormSurface(this);

            var lblTitle = new Label
            {
                Text = "Admin Sign In",
                AutoSize = true,
                Left = 30,
                Top = 20
            };
            ThemeManager.StyleLabel(lblTitle, isTitle: true);

            var lblPassword = new Label
            {
                Text = "Administrator Password",
                AutoSize = true,
                Left = 30,
                Top = 70
            };
            ThemeManager.StyleLabel(lblPassword);

            txtPassword = new TextBox
            {
                Top = 95,
                Left = 30,
                Width = 340,
                PasswordChar = '*'
            };
            ThemeManager.StyleTextField(txtPassword);
            
            // Login button
            btnLogin = new MaterialButton
            {
                Text = "Login",
                Top = 145,
                Left = 30,
                Width = 340
            };

            // Add controls to form
            Controls.AddRange(new Control[] { lblTitle, lblPassword, txtPassword, btnLogin });
        }
        
        /// <summary>
        /// Attaches event handlers to controls - CRITICAL for button responsiveness
        /// </summary>
        private void AttachEventHandlers()
        {
            // Explicit event handler registration
            btnLogin.Click += BtnLogin_Click;
            txtPassword.KeyPress += TxtPassword_KeyPress;
        }
        
        /// <summary>
        /// Login button click handler
        /// </summary>
        private void BtnLogin_Click(object? sender, EventArgs e)
        {
            string password = txtPassword.Text;
            string? envPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD");
            
            if (string.IsNullOrWhiteSpace(envPassword))
            {
                MessageBox.Show("Configuration error: Password not found in .env file", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            
            if (password == envPassword)
            {
                this.Hide();
                var teacherForm = new TeacherForm();
                teacherForm.FormClosed += (s2, e2) => this.Close();
                teacherForm.Show();
            }
            else
            {
                MessageBox.Show("Incorrect Password!", "Security", 
                    MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
        }
        
        /// <summary>
        /// Enter key support for password field
        /// </summary>
        private void TxtPassword_KeyPress(object? sender, KeyPressEventArgs e)
        {
            if (e.KeyChar != (char)Keys.Return)
                return;
            
            BtnLogin_Click(sender, EventArgs.Empty);
        }
    }
}
