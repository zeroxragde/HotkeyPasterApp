using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HotkeyPasterApp
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Iniciamos la aplicación sin un formulario principal visible, usando nuestro ApplicationContext
            Application.Run(new TrayAppContext());
        }
    }

    // Clase que maneja el ciclo de vida de la app en la bandeja (Tray)
    public class TrayAppContext : ApplicationContext
    {
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private HiddenHotkeyForm hotkeyForm;
        private AppSettings settings;
        private string configPath = "config.json";

        public TrayAppContext()
        {
            LoadSettings();

            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Configurar clave", null, OnConfigureText);
            trayMenu.Items.Add("Configurar Combinacion", null, OnConfigureHotkey);
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add("Salir", null, OnExit);

            // Se cambió la forma de cargar el icono para evitar el error CS0234 de la imagen
            Icon appIcon;
            try
            {
                // Intenta cargar el icono directamente desde la carpeta del ejecutable
                appIcon = new Icon("app.ico");
            }
            catch
            {
                // Si no encuentra el archivo "MiIcono.ico" en la carpeta, usa el icono por defecto del .exe
                appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }

            trayIcon = new NotifyIcon()
            {
                Icon = appIcon,
                ContextMenuStrip = trayMenu,
                Visible = true,
                Text = "Hotkey Paster"
            };

            // El formulario oculto se encarga de interceptar las teclas globales
            hotkeyForm = new HiddenHotkeyForm(settings, this);
        }

        private void LoadSettings()
        {
            if (File.Exists(configPath))
            {
                try
                {
                    string json = File.ReadAllText(configPath);
                    settings = JsonSerializer.Deserialize<AppSettings>(json);
                }
                catch { settings = new AppSettings(); }
            }
            else
            {
                settings = new AppSettings();
            }
        }

        public void SaveSettings()
        {
            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(configPath, json);
        }

        private void OnConfigureText(object sender, EventArgs e)
        {
            string currentText = CryptoHelper.Decrypt(settings.EncryptedText);
            string newText = PromptDialog.Show("Configurar Clave (Texto a pegar)", "Introduce el texto:", currentText);

            if (newText != null)
            {
                settings.EncryptedText = CryptoHelper.Encrypt(newText);
                SaveSettings();
            }
        }

        private void OnConfigureHotkey(object sender, EventArgs e)
        {
            using (var dialog = new HotkeyConfigDialog(settings))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    settings.Modifier = dialog.SelectedModifier;
                    settings.Key = dialog.SelectedKey;
                    SaveSettings();
                    hotkeyForm.RegisterConfiguredHotkey();
                }
            }
        }

        private void OnExit(object sender, EventArgs e)
        {
            trayIcon.Visible = false;
            hotkeyForm.Dispose();
            Application.Exit();
        }
    }

    // Configuración a guardar en JSON
    public class AppSettings
    {
        public int Modifier { get; set; } = 2; // Default: Control (2)
        public int Key { get; set; } = (int)Keys.F1; // Default: F1
        public string EncryptedText { get; set; } = "";
    }

    // Formulario oculto necesario para recibir los mensajes del sistema operativo (WM_HOTKEY)
    public class HiddenHotkeyForm : Form
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HOTKEY_ID = 9000;
        private const int WM_HOTKEY = 0x0312;
        private AppSettings settings;
        private TrayAppContext context;

        public HiddenHotkeyForm(AppSettings settings, TrayAppContext context)
        {
            this.settings = settings;
            this.context = context;
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.Size = new Size(0, 0);
            this.Load += (s, e) => RegisterConfiguredHotkey();

            // Evita que la ventana se muestre
            this.Opacity = 0;
            this.Show();
            this.Hide();
        }

        public void RegisterConfiguredHotkey()
        {
            UnregisterHotKey(this.Handle, HOTKEY_ID);
            if (settings.Key != 0)
            {
                RegisterHotKey(this.Handle, HOTKEY_ID, (uint)settings.Modifier, (uint)settings.Key);
            }
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
            {
                PasteText();
            }
        }

        private async void PasteText()
        {
            string plainText = CryptoHelper.Decrypt(settings.EncryptedText);
            if (!string.IsNullOrEmpty(plainText))
            {
                try
                {
                    // Respaldar el portapapeles actual
                    string backupText = Clipboard.ContainsText() ? Clipboard.GetText() : "";

                    Clipboard.SetText(plainText);

                    // Pequeña pausa para asegurar que el sistema registre el portapapeles y el usuario suelte las teclas
                    await Task.Delay(150);
                    SendKeys.SendWait("^v"); // Envía Ctrl + V
                    await Task.Delay(150);

                    // Restaurar el portapapeles (Opcional, puede fallar si otras apps lo bloquean)
                    if (!string.IsNullOrEmpty(backupText))
                        Clipboard.SetText(backupText);
                    else
                        Clipboard.Clear();
                }
                catch { /* Ignorar errores de acceso al portapapeles */ }
            }
        }

        protected override void Dispose(bool disposing)
        {
            UnregisterHotKey(this.Handle, HOTKEY_ID);
            base.Dispose(disposing);
        }
    }

    // Utilidad de Encriptación AES
    public static class CryptoHelper
    {
        // Clave segura compilada en la app (32 bytes para AES-256)
        private static readonly byte[] Key = Encoding.UTF8.GetBytes("Zx8R9!qP2#mN5*kL1@vC4$bM7^jX0&wY");

        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return "";

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.GenerateIV(); // Genera un Vector de Inicialización aleatorio

                using (var encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV))
                using (var msEncrypt = new MemoryStream())
                {
                    // Guardamos el IV al principio del stream para poder desencriptar después
                    msEncrypt.Write(aesAlg.IV, 0, aesAlg.IV.Length);
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    using (var swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(plainText);
                    }
                    return Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
        }

        public static string Decrypt(string cipherTextBase64)
        {
            if (string.IsNullOrEmpty(cipherTextBase64)) return "";

            try
            {
                byte[] fullCipher = Convert.FromBase64String(cipherTextBase64);
                using (Aes aesAlg = Aes.Create())
                {
                    aesAlg.Key = Key;
                    byte[] iv = new byte[aesAlg.BlockSize / 8];
                    byte[] cipherText = new byte[fullCipher.Length - iv.Length];

                    // Extraer el IV del inicio y el texto encriptado del resto
                    Array.Copy(fullCipher, iv, iv.Length);
                    Array.Copy(fullCipher, iv.Length, cipherText, 0, cipherText.Length);

                    aesAlg.IV = iv;

                    using (var decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV))
                    using (var msDecrypt = new MemoryStream(cipherText))
                    using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    using (var srDecrypt = new StreamReader(csDecrypt))
                    {
                        return srDecrypt.ReadToEnd();
                    }
                }
            }
            catch { return ""; } // Si la clave es errónea o está corrupto, retorna vacío
        }
    }

    // Diálogo sencillo para solicitar texto sin dependencias de VisualBasic
    public static class PromptDialog
    {
        public static string Show(string title, string promptText, string defaultValue)
        {
            Form form = new Form()
            {
                Width = 400,
                Height = 150,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = title,
                StartPosition = FormStartPosition.CenterScreen,
                MaximizeBox = false,
                MinimizeBox = false
            };
            Label textLabel = new Label() { Left = 20, Top = 20, Text = promptText, Width = 350 };
            TextBox textBox = new TextBox() { Left = 20, Top = 50, Width = 340, Text = defaultValue };
            Button confirmation = new Button() { Text = "Aceptar", Left = 260, Top = 80, Width = 100, DialogResult = DialogResult.OK };

            form.Controls.Add(textLabel);
            form.Controls.Add(textBox);
            form.Controls.Add(confirmation);
            form.AcceptButton = confirmation;

            return form.ShowDialog() == DialogResult.OK ? textBox.Text : null;
        }
    }

    // Formulario para capturar la combinación de teclas
    public class HotkeyConfigDialog : Form
    {
        public int SelectedModifier { get; private set; }
        public int SelectedKey { get; private set; }

        private ComboBox cmbModifier;
        private ComboBox cmbKey;

        public HotkeyConfigDialog(AppSettings settings)
        {
            this.Width = 300;
            this.Height = 180;
            this.Text = "Configurar Combinación";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;

            Label lblMod = new Label() { Text = "Modificador:", Left = 20, Top = 20, Width = 80 };
            cmbModifier = new ComboBox() { Left = 100, Top = 20, Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            // Modificadores nativos: Alt = 1, Ctrl = 2, Shift = 4, Win = 8
            cmbModifier.Items.Add(new { Text = "Ninguno", Value = 0 });
            cmbModifier.Items.Add(new { Text = "Alt", Value = 1 });
            cmbModifier.Items.Add(new { Text = "Ctrl", Value = 2 });
            cmbModifier.Items.Add(new { Text = "Shift", Value = 4 });
            cmbModifier.DisplayMember = "Text";
            cmbModifier.ValueMember = "Value";

            Label lblKey = new Label() { Text = "Tecla:", Left = 20, Top = 60, Width = 80 };
            cmbKey = new ComboBox() { Left = 100, Top = 60, Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };

            // Llenar con teclas comunes (letras y funciones)
            for (char c = 'A'; c <= 'Z'; c++) cmbKey.Items.Add(new { Text = c.ToString(), Value = (int)Enum.Parse(typeof(Keys), c.ToString()) });
            for (int i = 1; i <= 12; i++) cmbKey.Items.Add(new { Text = $"F{i}", Value = (int)Enum.Parse(typeof(Keys), $"F{i}") });
            cmbKey.DisplayMember = "Text";
            cmbKey.ValueMember = "Value";

            Button btnOk = new Button() { Text = "Guardar", Left = 150, Top = 100, Width = 100, DialogResult = DialogResult.OK };

            this.Controls.Add(lblMod);
            this.Controls.Add(cmbModifier);
            this.Controls.Add(lblKey);
            this.Controls.Add(cmbKey);
            this.Controls.Add(btnOk);

            // Seleccionar los valores actuales
            SetComboBoxValue(cmbModifier, settings.Modifier);
            SetComboBoxValue(cmbKey, settings.Key);

            this.FormClosing += (s, e) =>
            {
                if (this.DialogResult == DialogResult.OK)
                {
                    SelectedModifier = (int)((dynamic)cmbModifier.SelectedItem).Value;
                    SelectedKey = (int)((dynamic)cmbKey.SelectedItem).Value;
                }
            };
        }

        private void SetComboBoxValue(ComboBox cmb, int value)
        {
            foreach (dynamic item in cmb.Items)
            {
                if (item.Value == value)
                {
                    cmb.SelectedItem = item;
                    break;
                }
            }
            if (cmb.SelectedIndex == -1 && cmb.Items.Count > 0) cmb.SelectedIndex = 0;
        }
    }
}