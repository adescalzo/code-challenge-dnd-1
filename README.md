# Tech Challenge DnD-1 - Procesador de Datos de Sensores

## Descripción General

Solución desarrollada en .NET 9.0 para procesar datos de sensores y generar reportes de salida optimizados en múltiples formatos (CSV, XML)

## Arquitectura de la Solución

### Proyectos

**TechChallenge.Application**: Núcleo de la solución del challenge
- Implementa el procesamiento de datos de sensores con patrones de arquitectura limpia
- Incluye procesadores optimizados para CSV y XML con soporte para streaming

**TechChallenge.Application.Tests**: Suite completa de pruebas unitarias
- Tests de rendimiento para datasets grandes

**TechChallenge.Client**: Cliente de demostración y testing
- Aplicación de consola para ejecutar la solución completa
- Configuración mediante `process-config.json`

## Funcionalidades Principales

### Procesamiento de Datos
- **Lectura streaming**: Procesamiento de archivos JSON grandes evitando cargar todo en memoria
- **Validación robusta**: Verificación de archivos, rutas y formatos de entrada

### Formatos de Salida Optimizados

#### CSV Personalizado
- **Formato de dos secciones**:
  1. Métricas globales (ID sensor máximo, promedio global)
  2. Información detallada por zona (nombre, promedio, sensores activos)
- **Separador personalizado**: Utiliza punto y coma (`;`) para compatibilidad internacional

#### XML Estructurado
- **Serialización optimizada**: Utiliza XmlSerializer con DTOs

## Configuración y Uso

### Archivo de Configuración (`process-config.json`)
```json
{
  "InputFilePath": "Input/sensores.json",
  "OutputRequests": [
    {
      "OutputFilePath": "Output/CSV",
      "OutputType": "Csv"
    },
    {
      "OutputFilePath": "Output/XML",
      "OutputType": "Xml"
    }
  ]
}
```

### Ejecución
```bash
cd src/TechChallenge.Client
dotnet run
```

### Ejecución de Tests
```bash
cd src
dotnet test
```

## Tecnologías y Dependencias

- **.NET 9.0**: Framework principal
- **System.Text.Json**: Serialización JSON
- **System.IO.Abstractions**: Abstracción testeable del sistema de archivos
- **xUnit + FluentAssertions**: Framework de testing moderno
- **NSubstitute**: Mocking
