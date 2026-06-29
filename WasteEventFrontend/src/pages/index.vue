<template>
  <v-container class="index-page pa-0" fluid>
    <v-row class="index-layout ma-0" no-gutters>
      <v-col class="map-pane" cols="12" md="8">
        <OpenLayersMap ref="map" />
      </v-col>

      <v-col class="event-pane" cols="12" md="4">
        <div class="pa-4">
          <h2 class="text-h6 mb-3">Service-status</h2>
           <v-row density="comfortable">
            <v-col v-for="(count, status) in store.statusCounts" :key="status" cols="6">
              <v-card variant="tonal" class="pa-3">
                <div class="text-caption text-medium-emphasis">{{ status }}</div>
                <div class="text-h5">{{ count }}</div>
              </v-card>
            </v-col>
         </v-row>

          <h2 class="text-h6 mb-2 mt-5">Live-feed</h2>
          <v-list density="compact" class="feed-list">
            <v-list-item v-for="item in store.feed" :key="item.id" class="px-2">
              <template #prepend>
                <span class="text-caption text-medium-emphasis mr-2">{{ item.at }}</span>
              </template>
              <v-list-item-title class="text-body-2">{{ item.text }}</v-list-item-title>
            </v-list-item>
          </v-list>
        </div>
  
      </v-col>
      
    </v-row>
  </v-container>
</template>

<script lang="ts" setup>
  import { onBeforeUnmount, onMounted, ref, watch } from 'vue'
  import OpenLayersMap from '@/components/OpenLayersMap.vue'
  import { useEventStore } from '@/stores/useEventStore'

  const map = ref<InstanceType<typeof OpenLayersMap>>()
  const store = useEventStore()

  let eventSource: EventSource | undefined

  // Kobler en carId til det ikon der er tegnet for den på kortet.
  const truckIcons = new Map<number, { id: string }>()
  let hasZoomed = false

  // Tegn/opdater kort-ikoner ud fra storens trucks. watch reagerer når
  // storen ændrer sig, så kortet altid afspejler nuværende state.
  watch(
    () => store.trucks,
    trucks => {
      for (const truck of trucks.values()) {
        const point = { lon: truck.lon, lat: truck.lat }
        const rotation = truck.heading == null ? undefined : (truck.heading * Math.PI) / 180
        const existing = truckIcons.get(truck.carId)

        if (existing) {
          map.value?.updateIcon(existing, point, rotation == null ? undefined : { rotation })
          } else {
          const iconRef = map.value?.drawIcon(point, {
            iconUrl: '/truck.svg',
            rotation: rotation ?? 0,
            scale: 1.2,
          })
          if (iconRef) truckIcons.set(truck.carId, iconRef)

          // Zoom til aktivt område én gang, når den første bil dukker op.
          if (!hasZoomed) {
            hasZoomed = true
            map.value?.fitToBoundary(
              { minLon: 12.3, minLat: 55.6, maxLon: 12.7, maxLat: 55.8 },
              { duration: 600, maxZoom: 12 },
            )
          }
        }
      }
    },
    { deep: true },
  )

  // Tegn en markør for hver ny request. requests-arrayet vokser kun,
  // så vi tegner blot dem vi ikke har set endnu.
  let drawnRequests = 0

  watch(
    () => store.requests.length,
    () => {
      for (let i = drawnRequests; i < store.requests.length; i++) {
        const r = store.requests[i]
        if (r.lon == null || r.lat == null) continue
        map.value?.drawIcon(
          { lon: r.lon, lat: r.lat },
          { iconUrl: '/request.svg', scale: 1.1 },
        )
      }
      drawnRequests = store.requests.length
    },
  )

  onMounted(() => {
    eventSource = new EventSource('/api/events/stream')

    eventSource.onmessage = event => {
      const data = JSON.parse(event.data)

      switch (data.eventType) {
        case 'RoutePositionUpdate':
          store.upsertTruck(data.payload)
          break
        case 'RequestCreated':
          store.addRequest(data.payload)
          break
        case 'ServiceUpdate':
          store.applyServiceUpdate(data.payload)
          break
      }
    }

    eventSource.onerror = () => {
      console.error('SSE connection error')
    }
  })

  onBeforeUnmount(() => {
    eventSource?.close()
  })
</script>

<style scoped>
  .index-page,
  .index-layout {
    height: 100vh;
    min-height: 0;
    width: 100vw;
  }

  .map-pane {
    height: 100%;
    min-height: 0;
  }

  .event-pane {
    background: rgb(var(--v-theme-surface));
    border-left: 1px solid rgba(var(--v-theme-on-surface), 0.12);
    height: 100%;
    min-height: 0;
    overflow: auto;
  }

  .feed-list {
    max-height: 45vh;
    overflow-y: auto;
  }

  @media (max-width: 959px) {
    .map-pane {
      height: 60vh;
    }

    .event-pane {
      border-left: 0;
      border-top: 1px solid rgba(var(--v-theme-on-surface), 0.12);
      height: 40vh;
    }
  }
</style>
