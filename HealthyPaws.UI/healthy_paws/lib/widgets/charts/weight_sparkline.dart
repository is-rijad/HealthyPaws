import 'package:material_ui/material_ui.dart';

/// Minimalan linijski graf za niz vrijednosti (hronološki, od najstarije).
class WeightSparkline extends StatelessWidget {
  final List<double> values;
  final double height;

  const WeightSparkline({super.key, required this.values, this.height = 56});

  @override
  Widget build(BuildContext context) {
    if (values.length < 2) {
      return SizedBox(
        height: height,
        child: const Center(child: Text("Nedovoljno mjerenja za graf.")),
      );
    }
    return SizedBox(
      height: height,
      width: double.infinity,
      child: CustomPaint(
        painter: _SparklinePainter(
          values: values,
          color: Theme.of(context).colorScheme.primary,
        ),
      ),
    );
  }
}

class _SparklinePainter extends CustomPainter {
  final List<double> values;
  final Color color;

  _SparklinePainter({required this.values, required this.color});

  @override
  void paint(Canvas canvas, Size size) {
    const padding = 6.0;
    final min = values.reduce((a, b) => a < b ? a : b);
    final max = values.reduce((a, b) => a > b ? a : b);
    final range = (max - min) == 0 ? 1.0 : (max - min);

    final points = <Offset>[
      for (var i = 0; i < values.length; i++)
        Offset(
          padding + (size.width - 2 * padding) * i / (values.length - 1),
          size.height -
              padding -
              (size.height - 2 * padding) * (values[i] - min) / range,
        ),
    ];

    final linePaint = Paint()
      ..color = color
      ..strokeWidth = 2
      ..style = PaintingStyle.stroke
      ..strokeJoin = StrokeJoin.round;
    final path = Path()..moveTo(points.first.dx, points.first.dy);
    for (final p in points.skip(1)) {
      path.lineTo(p.dx, p.dy);
    }
    canvas.drawPath(path, linePaint);

    final dotPaint = Paint()..color = color;
    for (final p in points) {
      canvas.drawCircle(p, 3, dotPaint);
    }
    canvas.drawCircle(points.last, 5, dotPaint);
  }

  @override
  bool shouldRepaint(_SparklinePainter old) =>
      old.values != values || old.color != color;
}
