using System;
using System.Windows.Forms;

namespace PLC {
    public static class AddPointDialog {
        public static PLC.IBase.IRegister ShowDialog () {
            Form form = new Form() {
                Width = 380,
                Height = 280,
                Text = "添加读写点",
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog
            };

            int y = 20;

            // 地址输入框
            form.Controls.Add(new Label { Text = "地址:", Left = 20, Top = y + 3, AutoSize = true });
            var txtAddr = new TextBox { Left = 90, Top = y, Width = 200 };
            form.Controls.Add(txtAddr);
            y += 40;

            // 目标值
            form.Controls.Add(new Label { Text = "目标值:", Left = 20, Top = y + 3, AutoSize = true });
            var txtTarget = new TextBox { Left = 90, Top = y, Width = 200, Text = "0" };
            form.Controls.Add(txtTarget);
            y += 40;

            // 描述
            form.Controls.Add(new Label { Text = "描述:", Left = 20, Top = y + 3, AutoSize = true });
            var txtDesc = new TextBox { Left = 90, Top = y, Width = 200 };
            form.Controls.Add(txtDesc);
            y += 50;

            // 类型选择
            form.Controls.Add(new Label { Text = "类型:", Left = 20, Top = y + 3, AutoSize = true });
            var cmbType = new ComboBox {
                Left = 90,
                Top = y,
                Width = 150,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbType.Items.AddRange(new object[] { "Int16", "UInt16", "Int32", "UInt32", "Coil" });
            cmbType.SelectedIndex = 0;
            form.Controls.Add(cmbType);
            y += 50;

            var btnOk = new Button { Text = "确定", Left = 120, Top = y, Width = 80, DialogResult = DialogResult.OK };
            form.Controls.Add(btnOk);
            form.AcceptButton = btnOk;

            if (form.ShowDialog() == DialogResult.OK) {
                // ==================== 关键：正确解析地址 ====================
                int targetValue = 0;
                int.TryParse(txtTarget.Text.Trim(), out targetValue);

                PLC.IBase.RegisterDataType dt = PLC.IBase.RegisterDataType.Int16;
                switch (cmbType.SelectedItem?.ToString()) {
                    case "UInt16": dt = PLC.IBase.RegisterDataType.UInt16; break;
                    case "Int32": dt = PLC.IBase.RegisterDataType.Int32; break;
                    case "UInt32": dt = PLC.IBase.RegisterDataType.UInt32; break;
                    case "Coil": dt = PLC.IBase.RegisterDataType.Coil; break;
                }

                return new PLC.IBase.IRegister {
                    Address = txtAddr.Text.Trim(),
                    TargetValue = targetValue,
                    Description = txtDesc.Text.Trim(),
                    DataType = dt,
                    CurrentValue = 0
                };
            }

            return null;
        }
    }
}