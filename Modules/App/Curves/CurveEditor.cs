﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Vixen.Module.App;
using Vixen.Services;
using Vixen.Sys;
using ZedGraph;

namespace VixenModules.App.Curves
{
	public partial class CurveEditor : Form
	{
		public CurveEditor()
		{
			InitializeComponent();

			zedGraphControl.GraphPane.XAxis.MajorGrid.IsVisible = true;
			zedGraphControl.GraphPane.XAxis.MajorGrid.Color = Color.Gray;
			zedGraphControl.GraphPane.XAxis.MajorGrid.DashOff = 4;
			zedGraphControl.GraphPane.XAxis.MajorGrid.DashOn = 2;

			zedGraphControl.GraphPane.YAxis.MajorGrid.IsVisible = true;
			zedGraphControl.GraphPane.YAxis.MajorGrid.Color = Color.Gray;
			zedGraphControl.GraphPane.YAxis.MajorGrid.DashOff = 4;
			zedGraphControl.GraphPane.YAxis.MajorGrid.DashOn = 2;

			zedGraphControl.EditModifierKeys = Keys.None;
			zedGraphControl.IsShowContextMenu = false;
			zedGraphControl.IsEnableSelection = false;
			zedGraphControl.EditButtons = System.Windows.Forms.MouseButtons.Left;
			zedGraphControl.GraphPane.XAxis.Scale.Min = 0;
			zedGraphControl.GraphPane.XAxis.Scale.Max = 100;
			zedGraphControl.GraphPane.YAxis.Scale.Min = 0;
			zedGraphControl.GraphPane.YAxis.Scale.Max = 100;
			zedGraphControl.GraphPane.XAxis.Title.IsVisible = false;
			zedGraphControl.GraphPane.YAxis.Title.IsVisible = false;
			zedGraphControl.GraphPane.Legend.IsVisible = false;
			zedGraphControl.GraphPane.Title.IsVisible = false;

			zedGraphControl.GraphPane.Fill = new Fill(SystemColors.Control);
			zedGraphControl.GraphPane.Border = new Border(SystemColors.Control, 0);

			zedGraphControl.GraphPane.AxisChange();
		}

		public CurveEditor(Curve curve)
			: this()
		{
			Curve = curve;
		}

		private Curve _curve;
		public Curve Curve
		{
			get { return _curve; }
			set
			{
				_curve = new Curve(value);
				PopulateFormWithCurve(_curve);
			}
		}

		private string _libraryCurveName;
		public string LibraryCurveName
		{
			get { return _libraryCurveName; }
			set
			{
				_libraryCurveName = value;
				PopulateFormWithCurve(Curve);
			}
		}

		private CurveLibrary _library;
		private CurveLibrary Library
		{
			get
			{
				if (_library == null)
					_library = ApplicationServices.Get<IAppModuleInstance>(CurveLibraryDescriptor.ModuleID) as CurveLibrary;

				return _library;
			}
		}

		public bool ReadonlyCurve { get; internal set; }

		private bool zedGraphControl_PreMouseMoveEvent(ZedGraphControl sender, MouseEventArgs e)
		{
			if (zedGraphControl.IsEditing) {
				PointPairList pointList = zedGraphControl.GraphPane.CurveList[0].Points as PointPairList;
				pointList.Sort();
			}
			return false;
		}

		private bool zedGraphControl_PostMouseMoveEvent(ZedGraphControl sender, MouseEventArgs e)
		{
			if (sender.DragEditingPair == null)
				return true;

			if (sender.DragEditingPair.X < 0)
				sender.DragEditingPair.X = 0;
			if (sender.DragEditingPair.X > 100)
				sender.DragEditingPair.X = 100;
			if (sender.DragEditingPair.Y < 0)
				sender.DragEditingPair.Y = 0;
			if (sender.DragEditingPair.Y > 100)
				sender.DragEditingPair.Y = 100;

			lblXValue.Text = sender.DragEditingPair.X.ToString();
			lblYValue.Text = sender.DragEditingPair.Y.ToString();
			// actually does nothing, just haven't changed the event handler definition
			return true;
		}

		private bool zedGraphControl_MouseDownEvent(ZedGraphControl sender, MouseEventArgs e)
		{
			if (ReadonlyCurve)
				return false;

			CurveItem curve;
			int dragPointIndex;

			// if CTRL is pressed, and we're not near a specific point, add a new point
			if (Control.ModifierKeys.HasFlag(Keys.Control) && !zedGraphControl.GraphPane.FindNearestPoint(e.Location, out curve, out dragPointIndex)) {
				// only add if we've actually clicked on the pane, so make sure the mouse is over it first
				if (zedGraphControl.MasterPane.FindPane(e.Location) != null) {
					PointPairList pointList = zedGraphControl.GraphPane.CurveList[0].Points as PointPairList;
					double newX, newY;
					zedGraphControl.GraphPane.ReverseTransform(e.Location, out newX, out newY);
					pointList.Insert(0, newX, newY);
					pointList.Sort();
					zedGraphControl.Invalidate();
				}
			}
			// if the ALT key was pressed, and we're near a point, delete it -- but only if there would be at least two points left
			if (Control.ModifierKeys.HasFlag(Keys.Alt) && zedGraphControl.GraphPane.FindNearestPoint(e.Location, out curve, out dragPointIndex)) {
				PointPairList pointList = zedGraphControl.GraphPane.CurveList[0].Points as PointPairList;
				if (pointList.Count > 2) {
					pointList.RemoveAt(dragPointIndex);
					pointList.Sort();
					zedGraphControl.Invalidate();
				}
			}

			return false;
		}

		private bool zedGraphControl_MouseUpEvent(ZedGraphControl sender, MouseEventArgs e)
		{
			return false;
		}

		private void PopulateFormWithCurve(Curve curve)
		{
			// if we're editing a curve from the library, treat it special
			if (curve.IsCurrentLibraryCurve) {
				zedGraphControl.GraphPane.CurveList.Clear();
				zedGraphControl.GraphPane.AddCurve("", curve.Points, Curve.ActiveCurveGridColor);
				if (LibraryCurveName == null) {
					labelCurve.Text = "This curve is a library curve.";
					Text = "Curve Editor: Library Curve";
				} else {
					labelCurve.Text = "This curve is the library curve: " + LibraryCurveName;
					Text = "Curve Editor: Library Curve " + LibraryCurveName;
				}

				zedGraphControl.IsEnableHEdit = true;
				zedGraphControl.IsEnableVEdit = true;
				ReadonlyCurve = false;
				buttonSaveCurveToLibrary.Enabled = false;
				buttonLoadCurveFromLibrary.Enabled = false;
				buttonUnlinkCurve.Enabled = false;
				buttonEditLibraryCurve.Enabled = false;
				labelInstructions1.Visible = true;
				labelInstructions2.Visible = true;

				zedGraphControl.GraphPane.Chart.Fill = new Fill(Color.AliceBlue);

			} else {
				if (curve.IsLibraryReference) {
					zedGraphControl.GraphPane.CurveList.Clear();
					zedGraphControl.GraphPane.AddCurve("", curve.Points, Curve.InactiveCurveGridColor);
					labelCurve.Text = "This curve is linked to the library curve: " + curve.LibraryReferenceName;
				} else {
					zedGraphControl.GraphPane.CurveList.Clear();
					zedGraphControl.GraphPane.AddCurve("", curve.Points, Curve.ActiveCurveGridColor);
					labelCurve.Text = "This curve is not linked to any in the library.";
				}

				zedGraphControl.IsEnableHEdit = !curve.IsLibraryReference;
				zedGraphControl.IsEnableVEdit = !curve.IsLibraryReference;
				ReadonlyCurve = curve.IsLibraryReference;
				buttonSaveCurveToLibrary.Enabled = !curve.IsLibraryReference;
				buttonLoadCurveFromLibrary.Enabled = true;
				buttonUnlinkCurve.Enabled = curve.IsLibraryReference;
				buttonEditLibraryCurve.Enabled = curve.IsLibraryReference;
				labelInstructions1.Visible = !curve.IsLibraryReference;
				labelInstructions2.Visible = !curve.IsLibraryReference;

				zedGraphControl.GraphPane.Chart.Fill = new Fill(SystemColors.Control);

				Text = "Curve Editor";
			}

			zedGraphControl.Invalidate();
		}

		private void buttonLoadCurveFromLibrary_Click(object sender, EventArgs e)
		{
			CurveLibrarySelector selector = new CurveLibrarySelector();
			if (selector.ShowDialog() == System.Windows.Forms.DialogResult.OK && selector.SelectedItem != null) {
				// make a new curve that references the selected library curve, and set it to the current Curve
				Curve newCurve = new Curve(selector.SelectedItem.Item2);
				newCurve.LibraryReferenceName = selector.SelectedItem.Item1;
				newCurve.IsCurrentLibraryCurve = false;
				Curve = newCurve;
			}
		}

		private void buttonSaveCurveToLibrary_Click(object sender, EventArgs e)
		{
			Common.Controls.TextDialog dialog = new Common.Controls.TextDialog("Curve name?");

			while (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
				if (dialog.Response == "") {
					MessageBox.Show("Please enter a name.");
					continue;
				}

				if (Library.Contains(dialog.Response)) {
					DialogResult result = MessageBox.Show("There is already a curve with that name. Do you want to overwrite it?",
						"Overwrite curve?", MessageBoxButtons.YesNoCancel);
					if (result == System.Windows.Forms.DialogResult.Yes) {
						Library.AddCurve(dialog.Response, new Curve(Curve));
						break;
					} else if (result == System.Windows.Forms.DialogResult.Cancel) {
						break;
					}
				} else {
					Library.AddCurve(dialog.Response, new Curve(Curve));
					break;
				}
			}
		}

		private void buttonUnlinkCurve_Click(object sender, EventArgs e)
		{
			Curve.UnlinkFromLibraryCurve();
			PopulateFormWithCurve(Curve);
		}

		private void buttonEditLibraryCurve_Click(object sender, EventArgs e)
		{
			string libraryName = Curve.LibraryReferenceName;

			Library.EditLibraryCurve(libraryName);
	
			PopulateFormWithCurve(Curve);
		}
	}
}
