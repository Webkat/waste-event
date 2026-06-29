<template>
  <div ref="mapElement" class="open-layers-map" />
</template>

<script setup lang="ts">
  import Feature from 'ol/Feature'
  import LineString from 'ol/geom/LineString'
  import Point from 'ol/geom/Point'
  import TileLayer from 'ol/layer/Tile'
  import VectorLayer from 'ol/layer/Vector'
  import Map from 'ol/Map'
  import { fromLonLat, transformExtent } from 'ol/proj'
  import OSM from 'ol/source/OSM'
  import VectorSource from 'ol/source/Vector'
  import { Icon, Stroke, Style } from 'ol/style'
  import View from 'ol/View'
  import { onBeforeUnmount, onMounted, ref } from 'vue'
  import 'ol/ol.css'

  export type MapPoint = {
    lon: number
    lat: number
  }

  export type LineStyle = 'solid' | 'dotted'

  export type MapBoundary = {
    minLon: number
    minLat: number
    maxLon: number
    maxLat: number
  }

  export type FitBoundaryOptions = {
    duration?: number
    maxZoom?: number
    padding?: [number, number, number, number]
  }

  export type LineRef = {
    id: string
  }

  export type IconRef = {
    id: string
  }

  export type IconOptions = {
    iconUrl: string
    rotation?: number
    scale?: number
  }

  type StoredFeature = Feature<Point | LineString>

  const denmarkExtent = transformExtent([7.7, 54.4, 15.7, 58], 'EPSG:4326', 'EPSG:3857')
  const denmarkCenter = fromLonLat([10.4, 56.15])

  const mapElement = ref<HTMLElement>()
  const vectorSource = new VectorSource<StoredFeature>()
  const features = new globalThis.Map<string, StoredFeature>()
  const iconOptions = new globalThis.Map<string, Required<IconOptions>>()

  let map: Map | undefined

  const toCoordinate = (point: MapPoint) => fromLonLat([point.lon, point.lat])

  const createId = (prefix: string) => `${prefix}-${crypto.randomUUID()}`

  function createLineStyle (style: LineStyle = 'solid') {
    return new Style({
      stroke: new Stroke({
        color: '#1565c0',
        lineDash: style === 'dotted' ? [2, 8] : undefined,
        width: 4,
      }),
      zIndex: 10,
    })
  }

  function createIconStyle ({ iconUrl, rotation = 0, scale = 1 }: IconOptions) {
    const size = 48 * scale

    return new Style({
      image: new Icon({
        anchor: [0.5, 0.5],
        height: size,
        rotateWithView: true,
        rotation,
        src: iconUrl,
        width: size,
      }),
      zIndex: 20,
    })
  }

  const getFeature = <T extends StoredFeature> (featureRef: LineRef | IconRef) => features.get(featureRef.id) as T | undefined

  function drawLine (points: MapPoint[], style: LineStyle = 'solid'): LineRef {
    const id = createId('line')
    const feature = new Feature({
      geometry: new LineString(points.map(point => toCoordinate(point))),
    })

    feature.setStyle(createLineStyle(style))
    vectorSource.addFeature(feature)
    features.set(id, feature)

    return { id }
  }

  function updateLine (lineRef: LineRef, points: MapPoint[], style?: LineStyle) {
    const feature = getFeature<Feature<LineString>>(lineRef)

    if (!feature) return

    feature.setGeometry(new LineString(points.map(point => toCoordinate(point))))

    if (style) {
      feature.setStyle(createLineStyle(style))
    }
  }

  function removeLine (lineRef: LineRef) {
    const feature = getFeature(lineRef)

    if (!feature) return

    vectorSource.removeFeature(feature)
    features.delete(lineRef.id)
  }

  function drawIcon (point: MapPoint, options: IconOptions): IconRef {
    const id = createId('icon')
    const nextOptions = {
      iconUrl: options.iconUrl,
      rotation: options.rotation ?? 0,
      scale: options.scale ?? 1,
    }
    const feature = new Feature({
      geometry: new Point(toCoordinate(point)),
    })

    feature.setStyle(createIconStyle(nextOptions))
    vectorSource.addFeature(feature)
    features.set(id, feature)
    iconOptions.set(id, nextOptions)

    return { id }
  }

  function updateIcon (iconRef: IconRef, point: MapPoint, options?: Partial<IconOptions>) {
    const feature = getFeature<Feature<Point>>(iconRef)

    if (!feature) return
    feature.setGeometry(new Point(toCoordinate(point)))

    if (options) {
      const currentOptions = iconOptions.get(iconRef.id)

      if (!currentOptions && !options.iconUrl) return

      const nextOptions = {
        iconUrl: options.iconUrl ?? currentOptions?.iconUrl ?? '',
        rotation: options.rotation ?? currentOptions?.rotation ?? 0,
        scale: options.scale ?? currentOptions?.scale ?? 1,
      }

      feature.setStyle(createIconStyle(nextOptions))
      iconOptions.set(iconRef.id, nextOptions)
    }
  }

  function removeIcon (iconRef: IconRef) {
    const feature = getFeature(iconRef)

    if (!feature) return
    vectorSource.removeFeature(feature)
    features.delete(iconRef.id)
    iconOptions.delete(iconRef.id)
  }

  function fitToBoundary (boundary: MapBoundary, options: FitBoundaryOptions = {}) {
    const view = map?.getView()

    if (!view) return

    const extent = transformExtent([
      boundary.minLon,
      boundary.minLat,
      boundary.maxLon,
      boundary.maxLat,
    ], 'EPSG:4326', 'EPSG:3857')

    view.fit(extent, {
      duration: options.duration ?? 300,
      maxZoom: options.maxZoom,
      padding: options.padding ?? [48, 48, 48, 48],
    })
  }

  onMounted(() => {
    if (!mapElement.value) return

    map = new Map({
      layers: [
        new TileLayer({
          source: new OSM(),
        }),
        new VectorLayer({
          source: vectorSource,
        }),
      ],
      target: mapElement.value,
      view: new View({
        center: denmarkCenter,
        extent: denmarkExtent,
        maxZoom: 18,
        minZoom: 7,
        zoom: 7,
      }),
    })
  })

  onBeforeUnmount(() => {
    map?.setTarget(undefined)
    map = undefined
    vectorSource.clear()
    features.clear()
    iconOptions.clear()
  })

  defineExpose({
    drawIcon,
    drawLine,
    fitToBoundary,
    removeIcon,
    removeLine,
    updateIcon,
    updateLine,
  })
</script>

<style scoped>
  .open-layers-map {
    height: 100%;
    min-height: 0;
    width: 100%;
  }
</style>
