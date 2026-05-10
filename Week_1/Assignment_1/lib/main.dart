// ============================================================
// main.dart
// Flutter Unit Converter App
// Course: MSCS-631 | University of the Cumberlands
// Author: Arun Bhaskar Gadde
// Description: A metric/imperial conversion app supporting
//   Distance, Weight, Temperature, and Volume conversions.
//   Built following Effective Dart coding conventions.
// ============================================================

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

// ── Entry point ─────────────────────────────────────────────
void main() {
  // Lock orientation to portrait for consistent layout
  WidgetsFlutterBinding.ensureInitialized();
  SystemChrome.setPreferredOrientations([
    DeviceOrientation.portraitUp,
    DeviceOrientation.portraitDown,
  ]);
  runApp(const ConverterApp());
}

// ── Root application widget ──────────────────────────────────
class ConverterApp extends StatelessWidget {
  const ConverterApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Unit Converter',
      debugShowCheckedModeBanner: false,
      theme: _buildTheme(),
      home: const ConversionHomePage(),
    );
  }

  /// Builds the app-wide Material 3 theme with a teal/navy palette.
  ThemeData _buildTheme() {
    return ThemeData(
      useMaterial3: true,
      colorScheme: ColorScheme.fromSeed(
        seedColor: const Color(0xFF1A6FA8),
        brightness: Brightness.dark,
        primary: const Color(0xFF4FC3F7),
        secondary: const Color(0xFF80DEEA),
        surface: const Color(0xFF0D1B2A),
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: const Color(0xFF1A2E42),
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: const BorderSide(color: Color(0xFF4FC3F7)),
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: const BorderSide(color: Color(0xFF2D4A63)),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: const BorderSide(color: Color(0xFF4FC3F7), width: 2),
        ),
        labelStyle: const TextStyle(color: Color(0xFF80DEEA)),
        hintStyle: TextStyle(color: Colors.white38),
      ),
      dropdownMenuTheme: DropdownMenuThemeData(
        menuStyle: MenuStyle(
          backgroundColor:
              WidgetStateProperty.all(const Color(0xFF1A2E42)),
        ),
      ),
      elevatedButtonTheme: ElevatedButtonThemeData(
        style: ElevatedButton.styleFrom(
          backgroundColor: const Color(0xFF1A6FA8),
          foregroundColor: Colors.white,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(12),
          ),
          padding:
              const EdgeInsets.symmetric(horizontal: 32, vertical: 14),
          textStyle: const TextStyle(
              fontSize: 16, fontWeight: FontWeight.w600),
        ),
      ),
    );
  }
}

// ── Conversion Category enum ─────────────────────────────────

/// Represents the four supported categories of unit conversion.
enum ConversionCategory {
  distance,
  weight,
  temperature,
  volume,
}

/// Extension to provide display labels for each category.
extension ConversionCategoryLabel on ConversionCategory {
  String get label {
    switch (this) {
      case ConversionCategory.distance:
        return 'Distance';
      case ConversionCategory.weight:
        return 'Weight';
      case ConversionCategory.temperature:
        return 'Temperature';
      case ConversionCategory.volume:
        return 'Volume';
    }
  }

  IconData get icon {
    switch (this) {
      case ConversionCategory.distance:
        return Icons.straighten_rounded;
      case ConversionCategory.weight:
        return Icons.fitness_center_rounded;
      case ConversionCategory.temperature:
        return Icons.thermostat_rounded;
      case ConversionCategory.volume:
        return Icons.local_drink_rounded;
    }
  }
}

// ── Unit model ───────────────────────────────────────────────

/// Represents a single unit of measurement (e.g., "Kilometers").
class MeasurementUnit {
  /// Human-readable name shown in the dropdown.
  final String name;

  /// Short abbreviation shown alongside the result (e.g., "km").
  final String abbreviation;

  /// Conversion factor relative to the SI base unit for the category.
  /// Temperature is handled separately via formula.
  final double? factor;

  const MeasurementUnit({
    required this.name,
    required this.abbreviation,
    this.factor,
  });
}

// ── Unit definitions ─────────────────────────────────────────

/// Centralized repository of all supported units, grouped by category.
///
/// Factors are relative to the SI base:
///   Distance  → metre      (1 m = 1.0)
///   Weight    → kilogram   (1 kg = 1.0)
///   Volume    → litre      (1 L = 1.0)
///   Temperature → handled via [ConversionEngine.convertTemperature]
class UnitRepository {
  // Distance units
  static const List<MeasurementUnit> distanceUnits = [
    MeasurementUnit(name: 'Metres',      abbreviation: 'm',   factor: 1.0),
    MeasurementUnit(name: 'Kilometres',  abbreviation: 'km',  factor: 1000.0),
    MeasurementUnit(name: 'Centimetres', abbreviation: 'cm',  factor: 0.01),
    MeasurementUnit(name: 'Millimetres', abbreviation: 'mm',  factor: 0.001),
    MeasurementUnit(name: 'Miles',       abbreviation: 'mi',  factor: 1609.344),
    MeasurementUnit(name: 'Yards',       abbreviation: 'yd',  factor: 0.9144),
    MeasurementUnit(name: 'Feet',        abbreviation: 'ft',  factor: 0.3048),
    MeasurementUnit(name: 'Inches',      abbreviation: 'in',  factor: 0.0254),
  ];

  // Weight units
  static const List<MeasurementUnit> weightUnits = [
    MeasurementUnit(name: 'Kilograms',   abbreviation: 'kg',  factor: 1.0),
    MeasurementUnit(name: 'Grams',       abbreviation: 'g',   factor: 0.001),
    MeasurementUnit(name: 'Milligrams',  abbreviation: 'mg',  factor: 0.000001),
    MeasurementUnit(name: 'Pounds',      abbreviation: 'lb',  factor: 0.45359237),
    MeasurementUnit(name: 'Ounces',      abbreviation: 'oz',  factor: 0.02834952),
    MeasurementUnit(name: 'Tonnes',      abbreviation: 't',   factor: 1000.0),
    MeasurementUnit(name: 'Stone',       abbreviation: 'st',  factor: 6.35029318),
  ];

  // Temperature units (factor is unused; formulas used instead)
  static const List<MeasurementUnit> temperatureUnits = [
    MeasurementUnit(name: 'Celsius',    abbreviation: '°C'),
    MeasurementUnit(name: 'Fahrenheit', abbreviation: '°F'),
    MeasurementUnit(name: 'Kelvin',     abbreviation: 'K'),
  ];

  // Volume units
  static const List<MeasurementUnit> volumeUnits = [
    MeasurementUnit(name: 'Litres',         abbreviation: 'L',   factor: 1.0),
    MeasurementUnit(name: 'Millilitres',    abbreviation: 'mL',  factor: 0.001),
    MeasurementUnit(name: 'Cubic Metres',   abbreviation: 'm³',  factor: 1000.0),
    MeasurementUnit(name: 'Gallons (US)',   abbreviation: 'gal', factor: 3.785411784),
    MeasurementUnit(name: 'Quarts (US)',    abbreviation: 'qt',  factor: 0.946352946),
    MeasurementUnit(name: 'Pints (US)',     abbreviation: 'pt',  factor: 0.473176473),
    MeasurementUnit(name: 'Fluid Oz (US)', abbreviation: 'fl oz', factor: 0.029573529),
    MeasurementUnit(name: 'Cups (US)',     abbreviation: 'cup', factor: 0.236588236),
  ];

  /// Returns the unit list for the given [category].
  static List<MeasurementUnit> unitsFor(ConversionCategory category) {
    switch (category) {
      case ConversionCategory.distance:
        return distanceUnits;
      case ConversionCategory.weight:
        return weightUnits;
      case ConversionCategory.temperature:
        return temperatureUnits;
      case ConversionCategory.volume:
        return volumeUnits;
    }
  }
}

// ── Conversion engine ────────────────────────────────────────

/// Stateless service class that performs unit conversions.
///
/// For factor-based units: converts [value] from [from] to [to]
/// by normalising to the SI base unit then scaling to the target.
///
/// Temperature uses dedicated formulas (Celsius ↔ Fahrenheit ↔ Kelvin).
class ConversionEngine {
  ConversionEngine._(); // prevent instantiation

  /// Converts [value] from unit [from] to unit [to].
  /// Returns [null] if the input cannot be parsed.
  static double? convert({
    required double value,
    required MeasurementUnit from,
    required MeasurementUnit to,
    required ConversionCategory category,
  }) {
    if (category == ConversionCategory.temperature) {
      return _convertTemperature(value, from.name, to.name);
    }

    // Factor-based conversion: value * fromFactor / toFactor
    final double fromFactor = from.factor!;
    final double toFactor = to.factor!;
    return value * fromFactor / toFactor;
  }

  /// Converts temperature between Celsius, Fahrenheit, and Kelvin.
  static double _convertTemperature(
      double value, String fromName, String toName) {
    if (fromName == toName) return value;

    // Step 1: convert to Celsius as the intermediate pivot
    double celsius;
    switch (fromName) {
      case 'Celsius':
        celsius = value;
        break;
      case 'Fahrenheit':
        celsius = (value - 32) * 5 / 9;
        break;
      case 'Kelvin':
        celsius = value - 273.15;
        break;
      default:
        celsius = value;
    }

    // Step 2: convert from Celsius to the target unit
    switch (toName) {
      case 'Celsius':
        return celsius;
      case 'Fahrenheit':
        return celsius * 9 / 5 + 32;
      case 'Kelvin':
        return celsius + 273.15;
      default:
        return celsius;
    }
  }
}

// ── Home page widget ─────────────────────────────────────────

/// The main (and only) page of the app.
///
/// Maintains state for:
///   - selected conversion [category]
///   - selected [fromUnit] and [toUnit]
///   - raw [inputValue] from the text field
///   - computed [result]
class ConversionHomePage extends StatefulWidget {
  const ConversionHomePage({super.key});

  @override
  State<ConversionHomePage> createState() => _ConversionHomePageState();
}

class _ConversionHomePageState extends State<ConversionHomePage> {
  // ── State variables ──────────────────────────────────────
  ConversionCategory _category = ConversionCategory.distance;
  late List<MeasurementUnit> _units;
  late MeasurementUnit _fromUnit;
  late MeasurementUnit _toUnit;
  final TextEditingController _inputController = TextEditingController();
  String _result = '';
  bool _hasError = false;

  @override
  void initState() {
    super.initState();
    _initUnitsForCategory(_category);
  }

  @override
  void dispose() {
    _inputController.dispose();
    super.dispose();
  }

  /// Initialises unit lists and resets selections when the category changes.
  void _initUnitsForCategory(ConversionCategory category) {
    _units = UnitRepository.unitsFor(category);
    _fromUnit = _units.first;
    // Default "to" unit is the second item for a useful starting conversion
    _toUnit = _units.length > 1 ? _units[1] : _units.first;
    _result = '';
    _hasError = false;
    _inputController.clear();
  }

  /// Called when the user taps the Convert button.
  void _performConversion() {
    final String rawInput = _inputController.text.trim();
    if (rawInput.isEmpty) {
      setState(() {
        _result = '';
        _hasError = false;
      });
      return;
    }

    final double? inputValue = double.tryParse(rawInput);
    if (inputValue == null) {
      setState(() {
        _hasError = true;
        _result = 'Please enter a valid number.';
      });
      return;
    }

    final double? converted = ConversionEngine.convert(
      value: inputValue,
      from: _fromUnit,
      to: _toUnit,
      category: _category,
    );

    setState(() {
      _hasError = false;
      if (converted == null) {
        _result = 'Conversion error.';
        _hasError = true;
      } else {
        // Format: up to 6 significant decimal places, strip trailing zeros
        _result =
            '${_formatResult(converted)} ${_toUnit.abbreviation}';
      }
    });
  }

  /// Swaps the from and to units.
  void _swapUnits() {
    setState(() {
      final MeasurementUnit temp = _fromUnit;
      _fromUnit = _toUnit;
      _toUnit = temp;
      // Re-run conversion if there is already a value
      if (_inputController.text.isNotEmpty) {
        _performConversion();
      }
    });
  }

  /// Formats a double result to a readable string (max 6 decimal places).
  String _formatResult(double value) {
    if (value == value.roundToDouble()) {
      return value.toStringAsFixed(0);
    }
    // Remove trailing zeros
    String s = value.toStringAsFixed(6);
    s = s.replaceAll(RegExp(r'0+$'), '');
    s = s.replaceAll(RegExp(r'\.$'), '');
    return s;
  }

  // ── Build ────────────────────────────────────────────────
  @override
  Widget build(BuildContext context) {
    final ColorScheme colors = Theme.of(context).colorScheme;

    return Scaffold(
      backgroundColor: colors.surface,
      appBar: _buildAppBar(colors),
      body: SingleChildScrollView(
        padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            _buildCategorySelector(colors),
            const SizedBox(height: 24),
            _buildConversionCard(colors),
            const SizedBox(height: 20),
            _buildResultCard(colors),
            const SizedBox(height: 24),
            _buildInfoCard(colors),
          ],
        ),
      ),
    );
  }

  AppBar _buildAppBar(ColorScheme colors) {
    return AppBar(
      backgroundColor: const Color(0xFF0A1520),
      elevation: 0,
      title: Row(
        children: [
          Icon(Icons.swap_horiz_rounded, color: colors.primary),
          const SizedBox(width: 10),
          Text(
            'Unit Converter',
            style: TextStyle(
              color: colors.primary,
              fontSize: 22,
              fontWeight: FontWeight.bold,
              letterSpacing: 0.5,
            ),
          ),
        ],
      ),
    );
  }

  /// Horizontally scrollable category chip row.
  Widget _buildCategorySelector(ColorScheme colors) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          'SELECT CATEGORY',
          style: TextStyle(
            color: colors.secondary,
            fontSize: 11,
            fontWeight: FontWeight.w700,
            letterSpacing: 1.5,
          ),
        ),
        const SizedBox(height: 10),
        SingleChildScrollView(
          scrollDirection: Axis.horizontal,
          child: Row(
            children: ConversionCategory.values.map((cat) {
              final bool selected = cat == _category;
              return Padding(
                padding: const EdgeInsets.only(right: 10),
                child: ChoiceChip(
                  label: Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Icon(
                        cat.icon,
                        size: 16,
                        color: selected
                            ? const Color(0xFF0D1B2A)
                            : colors.primary,
                      ),
                      const SizedBox(width: 6),
                      Text(cat.label),
                    ],
                  ),
                  selected: selected,
                  onSelected: (_) {
                    setState(() {
                      _category = cat;
                      _initUnitsForCategory(cat);
                    });
                  },
                  selectedColor: colors.primary,
                  backgroundColor: const Color(0xFF1A2E42),
                  labelStyle: TextStyle(
                    color: selected
                        ? const Color(0xFF0D1B2A)
                        : colors.primary,
                    fontWeight: FontWeight.w600,
                  ),
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(8),
                  ),
                  side: BorderSide(
                    color: selected ? colors.primary : const Color(0xFF2D4A63),
                  ),
                ),
              );
            }).toList(),
          ),
        ),
      ],
    );
  }

  /// Card containing the input field, unit dropdowns, and convert button.
  Widget _buildConversionCard(ColorScheme colors) {
    return Container(
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: const Color(0xFF111E2D),
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: const Color(0xFF2D4A63)),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withOpacity(0.3),
            blurRadius: 12,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          // Input value field
          TextField(
            controller: _inputController,
            keyboardType:
                const TextInputType.numberWithOptions(decimal: true, signed: true),
            style: const TextStyle(
                color: Colors.white, fontSize: 18, fontWeight: FontWeight.w500),
            decoration: InputDecoration(
              labelText: 'Enter value',
              hintText: '0.00',
              prefixIcon:
                  Icon(_category.icon, color: colors.primary),
              suffixText: _fromUnit.abbreviation,
              suffixStyle: TextStyle(
                  color: colors.secondary,
                  fontWeight: FontWeight.bold,
                  fontSize: 16),
            ),
            onSubmitted: (_) => _performConversion(),
          ),
          const SizedBox(height: 20),

          // From / Swap / To row
          Row(
            children: [
              // FROM dropdown
              Expanded(child: _buildUnitDropdown(
                label: 'From',
                selectedUnit: _fromUnit,
                onChanged: (unit) => setState(() {
                  _fromUnit = unit!;
                  _result = '';
                }),
                colors: colors,
              )),
              const SizedBox(width: 8),

              // Swap button
              Column(
                children: [
                  const SizedBox(height: 14),
                  InkWell(
                    onTap: _swapUnits,
                    borderRadius: BorderRadius.circular(50),
                    child: Container(
                      padding: const EdgeInsets.all(8),
                      decoration: BoxDecoration(
                        color: const Color(0xFF1A2E42),
                        shape: BoxShape.circle,
                        border: Border.all(color: colors.primary),
                      ),
                      child: Icon(Icons.swap_horiz_rounded,
                          color: colors.primary, size: 22),
                    ),
                  ),
                ],
              ),
              const SizedBox(width: 8),

              // TO dropdown
              Expanded(child: _buildUnitDropdown(
                label: 'To',
                selectedUnit: _toUnit,
                onChanged: (unit) => setState(() {
                  _toUnit = unit!;
                  _result = '';
                }),
                colors: colors,
              )),
            ],
          ),
          const SizedBox(height: 20),

          // Convert button
          ElevatedButton.icon(
            onPressed: _performConversion,
            icon: const Icon(Icons.calculate_rounded),
            label: const Text('Convert'),
          ),
        ],
      ),
    );
  }

  /// Builds a labeled dropdown for unit selection.
  Widget _buildUnitDropdown({
    required String label,
    required MeasurementUnit selectedUnit,
    required ValueChanged<MeasurementUnit?> onChanged,
    required ColorScheme colors,
  }) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label.toUpperCase(),
          style: TextStyle(
              color: colors.secondary,
              fontSize: 10,
              letterSpacing: 1.4,
              fontWeight: FontWeight.w700),
        ),
        const SizedBox(height: 6),
        Container(
          padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
          decoration: BoxDecoration(
            color: const Color(0xFF1A2E42),
            borderRadius: BorderRadius.circular(10),
            border: Border.all(color: const Color(0xFF2D4A63)),
          ),
          child: DropdownButton<MeasurementUnit>(
            value: selectedUnit,
            isExpanded: true,
            underline: const SizedBox.shrink(),
            dropdownColor: const Color(0xFF1A2E42),
            style: const TextStyle(color: Colors.white, fontSize: 14),
            icon: Icon(Icons.keyboard_arrow_down_rounded,
                color: colors.primary),
            items: _units.map((unit) {
              return DropdownMenuItem<MeasurementUnit>(
                value: unit,
                child: Text(unit.name,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(fontSize: 13)),
              );
            }).toList(),
            onChanged: onChanged,
          ),
        ),
      ],
    );
  }

  /// Displays the conversion result or an error message.
  Widget _buildResultCard(ColorScheme colors) {
    return AnimatedContainer(
      duration: const Duration(milliseconds: 300),
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: _hasError
            ? const Color(0xFF2D1515)
            : const Color(0xFF0D2035),
        borderRadius: BorderRadius.circular(16),
        border: Border.all(
          color: _hasError
              ? Colors.redAccent
              : (_result.isNotEmpty ? colors.primary : const Color(0xFF2D4A63)),
          width: _result.isNotEmpty ? 1.5 : 1,
        ),
      ),
      child: Column(
        children: [
          Text(
            'RESULT',
            style: TextStyle(
              color: colors.secondary,
              fontSize: 11,
              letterSpacing: 2,
              fontWeight: FontWeight.w700,
            ),
          ),
          const SizedBox(height: 8),
          _result.isEmpty
              ? Text(
                  '—',
                  style: TextStyle(
                      color: Colors.white24,
                      fontSize: 36,
                      fontWeight: FontWeight.w300),
                )
              : Text(
                  _result,
                  textAlign: TextAlign.center,
                  style: TextStyle(
                    color:
                        _hasError ? Colors.redAccent : colors.primary,
                    fontSize: _hasError ? 16 : 32,
                    fontWeight: FontWeight.bold,
                    letterSpacing: 0.5,
                  ),
                ),
          if (!_hasError && _result.isNotEmpty)
            Padding(
              padding: const EdgeInsets.only(top: 6),
              child: Text(
                '${_inputController.text} ${_fromUnit.abbreviation} = $_result',
                style: TextStyle(
                    color: Colors.white38, fontSize: 13),
              ),
            ),
        ],
      ),
    );
  }

  /// A small informational card showing quick reference facts.
  Widget _buildInfoCard(ColorScheme colors) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: const Color(0xFF0A1520),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: const Color(0xFF1E3A50)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            'QUICK REFERENCE  — ${_category.label.toUpperCase()}',
            style: TextStyle(
                color: colors.secondary,
                fontSize: 10,
                letterSpacing: 1.4,
                fontWeight: FontWeight.w700),
          ),
          const SizedBox(height: 10),
          ..._buildQuickReference(),
        ],
      ),
    );
  }

  /// Returns quick-reference conversion hints for the active category.
  List<Widget> _buildQuickReference() {
    final Map<ConversionCategory, List<String>> hints = {
      ConversionCategory.distance: [
        '1 mile = 1.609 km',
        '1 foot = 30.48 cm',
        '1 inch = 2.54 cm',
        '1 yard = 0.914 m',
      ],
      ConversionCategory.weight: [
        '1 kg = 2.205 lb',
        '1 lb = 453.59 g',
        '1 stone = 6.35 kg',
        '1 tonne = 1,000 kg',
      ],
      ConversionCategory.temperature: [
        '0 °C = 32 °F = 273.15 K',
        '100 °C = 212 °F = 373.15 K',
        '-40 °C = -40 °F',
        'Body temp ≈ 37 °C = 98.6 °F',
      ],
      ConversionCategory.volume: [
        '1 US gallon = 3.785 L',
        '1 L = 33.814 fl oz',
        '1 cup = 236.6 mL',
        '1 quart = 0.946 L',
      ],
    };

    return (hints[_category] ?? []).map((hint) {
      return Padding(
        padding: const EdgeInsets.only(bottom: 4),
        child: Row(
          children: [
            const Icon(Icons.arrow_right_rounded,
                color: Color(0xFF4FC3F7), size: 18),
            const SizedBox(width: 4),
            Text(hint,
                style: const TextStyle(
                    color: Colors.white60, fontSize: 13)),
          ],
        ),
      );
    }).toList();
  }
}
