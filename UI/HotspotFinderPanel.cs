using EliteDataRelay.Services;
using EliteDataRelay.Models;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace EliteDataRelay.UI
{
    public class HotspotFinderPanel : UserControl
    {
        private readonly HotspotFinderService _service;

        private TextBox _txtMineral = null!;
        private TextBox _txtRingType = null!;
        private TextBox _txtSystem = null!;
        private NumericUpDown _numMaxDist = null!;
        private Button _btnSearch = null!;
        private ListView _results = null!;

        public HotspotFinderPanel(HotspotFinderService service)
        {
            _service = service;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Dock = DockStyle.Fill;
            BackColor = Color.FromArgb(249, 250, 251);

            var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 38, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(6, 6, 6, 0) };
            _txtMineral = new TextBox { Width = 140, PlaceholderText = "Mineral (e.g., LTD)" };
            _txtRingType = new TextBox { Width = 120, PlaceholderText = "Ring type" };
            _txtSystem = new TextBox { Width = 160, PlaceholderText = "System contains" };
            _numMaxDist = new NumericUpDown { Width = 80, Minimum = 0, Maximum = 100000, DecimalPlaces = 0, Increment = 1000, Value = 0 };
            _btnSearch = new Button { Text = "Search", AutoSize = true };
            _btnSearch.Click += (s, e) => RunSearch();
            top.Controls.AddRange(new Control[] { new Label { Text = "Mineral:", AutoSize = true, Padding = new Padding(0,8,6,0) }, _txtMineral,
                new Label { Text = "Ring:", AutoSize = true, Padding = new Padding(12,8,6,0)}, _txtRingType,
                new Label { Text = "System:", AutoSize = true, Padding = new Padding(12,8,6,0)}, _txtSystem,
                new Label { Text = "Max Ls:", AutoSize = true, Padding = new Padding(12,8,6,0)}, _numMaxDist, _btnSearch });

            _results = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, GridLines = false };
            _results.Columns.Add("System", 200);
            _results.Columns.Add("Body", 180);
            _results.Columns.Add("Ring", 120);
            _results.Columns.Add("Mineral", 120);
            _results.Columns.Add("Dist Ls", 80);

            Controls.Add(_results);
            Controls.Add(top);
        }

        private void RunSearch()
        {
            _results.BeginUpdate();
            _results.Items.Clear();

            var list = _service.Search(new HotspotSearchCriteria
            {
                Mineral = _txtMineral.Text,
                RingType = _txtRingType.Text,
                SystemContains = _txtSystem.Text,
                MaxDistance = _numMaxDist.Value == 0 ? null : (double)_numMaxDist.Value
            });

            foreach (var h in list)
            {
                var lvi = new ListViewItem(new[] { h.StarSystem, h.Body, h.RingType, h.Mineral, double.IsNaN(h.DistanceFromStar) ? "" : h.DistanceFromStar.ToString("N0") });
                _results.Items.Add(lvi);
            }

            if (_results.Columns.Count > 0)
            {
                _results.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            }
            _results.EndUpdate();
        }
    }
}
