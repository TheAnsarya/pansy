using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Pansy.Core;

namespace Pansy.UI.Controls;

/// <summary>
/// Renders a call graph visualization on a Canvas control
/// </summary>
public class CallGraphRenderer {
	private readonly Canvas _canvas;
	private double _zoom = 1.0;
	private readonly Dictionary<uint, GraphNode> _nodes = new();
	private readonly List<GraphEdge> _edges = new();

	// Layout constants
	private const double NodeWidth = 120;
	private const double NodeHeight = 40;
	private const double HorizontalSpacing = 60;
	private const double VerticalSpacing = 80;
	private const double StartX = 50;
	private const double StartY = 50;

	// Colors for different reference types
	private static readonly IBrush JsrBrush = new SolidColorBrush(Color.Parse("#2196F3")); // Blue
	private static readonly IBrush JmpBrush = new SolidColorBrush(Color.Parse("#4CAF50")); // Green
	private static readonly IBrush BranchBrush = new SolidColorBrush(Color.Parse("#FF9800")); // Orange
	private static readonly IBrush ReadBrush = new SolidColorBrush(Color.Parse("#9C27B0")); // Purple
	private static readonly IBrush WriteBrush = new SolidColorBrush(Color.Parse("#F44336")); // Red
	private static readonly IBrush DataRefBrush = new SolidColorBrush(Color.Parse("#607D8B")); // Gray
	private static readonly IBrush NodeFillBrush = new SolidColorBrush(Color.Parse("#E3F2FD")); // Light blue
	private static readonly IBrush NodeBorderBrush = new SolidColorBrush(Color.Parse("#1976D2")); // Dark blue
	private static readonly IBrush TextBrush = Brushes.Black;

	public CallGraphRenderer(Canvas canvas) {
		_canvas = canvas;
	}

	public double Zoom {
		get => _zoom;
		set {
			_zoom = Math.Clamp(value, 0.25, 2.0);
			RefreshLayout();
		}
	}

	/// <summary>
	/// Renders the call graph from cross-references and symbols
	/// </summary>
	public void Render(
		IEnumerable<CrossReference> crossRefs,
		IReadOnlyDictionary<int, string> symbols,
		IEnumerable<uint> subroutines) {
		_canvas.Children.Clear();
		_nodes.Clear();
		_edges.Clear();

		var crossRefList = crossRefs.ToList();
		var subroutineSet = subroutines.ToHashSet();

		// Create nodes for all subroutines
		foreach (var addr in subroutineSet) {
			var name = symbols.TryGetValue((int)addr, out var sym) ? sym : $"${addr:x4}";
			_nodes[addr] = new GraphNode(addr, name);
		}

		// Create edges from cross-references (only JSR/JMP for call graph)
		foreach (var xref in crossRefList.Where(x =>
			x.Type == CrossRefType.Jsr || x.Type == CrossRefType.Jmp)) {
			// Only show edges between known subroutines
			if (_nodes.ContainsKey(xref.To)) {
				_edges.Add(new GraphEdge(xref.From, xref.To, xref.Type));
			}
		}

		// Calculate layout
		CalculateLayout();

		// Draw edges first (so they're behind nodes)
		foreach (var edge in _edges) {
			DrawEdge(edge);
		}

		// Draw nodes
		foreach (var node in _nodes.Values) {
			DrawNode(node);
		}

		// Update canvas size based on nodes
		UpdateCanvasSize();
	}

	private void CalculateLayout() {
		// Simple hierarchical layout based on reference count
		var incomingCounts = _nodes.Keys.ToDictionary(
			k => k,
			k => _edges.Count(e => e.ToAddress == k));

		// Group nodes by "level" (0 incoming = root, etc.)
		var levels = new Dictionary<int, List<uint>>();
		foreach (var kvp in _nodes) {
			int level = Math.Min(incomingCounts[kvp.Key], 5); // Cap at 5 levels
			if (!levels.ContainsKey(level))
				levels[level] = new List<uint>();
			levels[level].Add(kvp.Key);
		}

		// Position nodes by level
		double y = StartY;
		foreach (var level in levels.OrderBy(l => l.Key)) {
			double x = StartX;
			foreach (var addr in level.Value.OrderBy(a => a)) {
				if (_nodes.TryGetValue(addr, out var node)) {
					node.X = x;
					node.Y = y;
					x += (NodeWidth + HorizontalSpacing) * _zoom;
				}
			}
			y += (NodeHeight + VerticalSpacing) * _zoom;
		}
	}

	private void DrawNode(GraphNode node) {
		var scaledWidth = NodeWidth * _zoom;
		var scaledHeight = NodeHeight * _zoom;

		// Node rectangle
		var rect = new Rectangle {
			Width = scaledWidth,
			Height = scaledHeight,
			Fill = NodeFillBrush,
			Stroke = NodeBorderBrush,
			StrokeThickness = 2,
			RadiusX = 4,
			RadiusY = 4
		};
		Canvas.SetLeft(rect, node.X);
		Canvas.SetTop(rect, node.Y);
		_canvas.Children.Add(rect);

		// Node text (address)
		var addrText = new TextBlock {
			Text = $"${node.Address:x4}",
			FontSize = 10 * _zoom,
			FontWeight = FontWeight.Bold,
			Foreground = TextBrush
		};
		Canvas.SetLeft(addrText, node.X + 4 * _zoom);
		Canvas.SetTop(addrText, node.Y + 2 * _zoom);
		_canvas.Children.Add(addrText);

		// Node text (name) - truncate if too long
		var displayName = node.Name.Length > 14 ? node.Name[..11] + "..." : node.Name;
		var nameText = new TextBlock {
			Text = displayName,
			FontSize = 9 * _zoom,
			Foreground = TextBrush
		};
		Canvas.SetLeft(nameText, node.X + 4 * _zoom);
		Canvas.SetTop(nameText, node.Y + 18 * _zoom);
		_canvas.Children.Add(nameText);
	}

	private void DrawEdge(GraphEdge edge) {
		// Find source and target nodes
		// Source might not be in _nodes if it's not a subroutine
		GraphNode? sourceNode = null;
		foreach (var node in _nodes.Values) {
			// Find closest node to from address
			if (node.Address == edge.FromAddress ||
			    (_nodes.Values.All(n => n.Address != edge.FromAddress) &&
			     Math.Abs((long)node.Address - (long)edge.FromAddress) < 0x100)) {
				sourceNode = node;
				break;
			}
		}

		if (sourceNode == null || !_nodes.TryGetValue(edge.ToAddress, out var targetNode))
			return;

		var scaledWidth = NodeWidth * _zoom;
		var scaledHeight = NodeHeight * _zoom;

		// Calculate edge endpoints (center bottom of source, center top of target)
		double x1 = sourceNode.X + scaledWidth / 2;
		double y1 = sourceNode.Y + scaledHeight;
		double x2 = targetNode.X + scaledWidth / 2;
		double y2 = targetNode.Y;

		// Get brush based on type
		var brush = GetBrushForType(edge.Type);

		// Draw line
		var line = new Line {
			StartPoint = new Point(x1, y1),
			EndPoint = new Point(x2, y2),
			Stroke = brush,
			StrokeThickness = 2 * _zoom
		};
		_canvas.Children.Add(line);

		// Draw arrowhead
		DrawArrowhead(x2, y2, x1, y1, brush);
	}

	private void DrawArrowhead(double tipX, double tipY, double fromX, double fromY, IBrush brush) {
		double angle = Math.Atan2(tipY - fromY, tipX - fromX);
		double arrowLength = 10 * _zoom;
		double arrowAngle = Math.PI / 6; // 30 degrees

		var p1 = new Point(
			tipX - arrowLength * Math.Cos(angle - arrowAngle),
			tipY - arrowLength * Math.Sin(angle - arrowAngle));
		var p2 = new Point(
			tipX - arrowLength * Math.Cos(angle + arrowAngle),
			tipY - arrowLength * Math.Sin(angle + arrowAngle));

		var arrow = new Polygon {
			Points = new Points { new Point(tipX, tipY), p1, p2 },
			Fill = brush
		};
		_canvas.Children.Add(arrow);
	}

	private static IBrush GetBrushForType(CrossRefType type) {
		return type switch {
			CrossRefType.Jsr => JsrBrush,
			CrossRefType.Jmp => JmpBrush,
			CrossRefType.Branch => BranchBrush,
			CrossRefType.Read => ReadBrush,
			CrossRefType.Write => WriteBrush,
			_ => TextBrush
		};
	}

	private void RefreshLayout() {
		if (_nodes.Count > 0) {
			CalculateLayout();
			// Re-render by clearing and redrawing
			_canvas.Children.Clear();
			foreach (var edge in _edges) {
				DrawEdge(edge);
			}
			foreach (var node in _nodes.Values) {
				DrawNode(node);
			}
			UpdateCanvasSize();
		}
	}

	private void UpdateCanvasSize() {
		if (_nodes.Count == 0) return;

		double maxX = _nodes.Values.Max(n => n.X) + NodeWidth * _zoom + StartX;
		double maxY = _nodes.Values.Max(n => n.Y) + NodeHeight * _zoom + StartY;
		_canvas.Width = Math.Max(maxX, 800);
		_canvas.Height = Math.Max(maxY, 600);
	}

	/// <summary>
	/// Generates GraphViz DOT format representation
	/// </summary>
	public string GenerateDotFormat(IReadOnlyDictionary<int, string> symbols) {
		var sb = new System.Text.StringBuilder();
		sb.AppendLine("digraph CallGraph {");
		sb.AppendLine("    rankdir=TB;");
		sb.AppendLine("    node [shape=box, style=filled, fillcolor=lightblue];");
		sb.AppendLine();

		// Add nodes
		foreach (var node in _nodes.Values) {
			var label = symbols.TryGetValue((int)node.Address, out var name)
				? $"{name}\\n${node.Address:x4}"
				: $"${node.Address:x4}";
			sb.AppendLine($"    n{node.Address:x4} [label=\"{label}\"];");
		}
		sb.AppendLine();

		// Add edges
		foreach (var edge in _edges) {
			var color = edge.Type switch {
				CrossRefType.Jsr => "blue",
				CrossRefType.Jmp => "green",
				_ => "gray"
			};
			sb.AppendLine($"    n{edge.FromAddress:x4} -> n{edge.ToAddress:x4} [color={color}];");
		}

		sb.AppendLine("}");
		return sb.ToString();
	}

	private class GraphNode {
		public uint Address { get; }
		public string Name { get; }
		public double X { get; set; }
		public double Y { get; set; }

		public GraphNode(uint address, string name) {
			Address = address;
			Name = name;
		}
	}

	private class GraphEdge {
		public uint FromAddress { get; }
		public uint ToAddress { get; }
		public CrossRefType Type { get; }

		public GraphEdge(uint from, uint to, CrossRefType type) {
			FromAddress = from;
			ToAddress = to;
			Type = type;
		}
	}
}
