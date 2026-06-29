import { reactive } from 'vue'

export type ServiceStatus =
  | 'Possible' | 'Await' | 'Complete' | 'Canceled' | 'CantComplete'

export interface RoutePositionPayload {
  carId: number
  deviceId: number
  routeId: number
  lon: number | null
  lat: number | null
  speed: number
  heading: number | null
}

export interface RequestCreatedPayload {
  requestId: number
  locationId: number
  serviceId: number
  lon: number | null
  lat: number | null
}

export interface ServiceUpdatePayload {
  serviceId: number
  locationId: number
  status: ServiceStatus
  routeId: number | null
  handledByCarId: number | null
  handledByDeviceId: number | null
}

interface TruckState {
  carId: number
  lon: number
  lat: number
  heading: number | null
  speed: number
  routeId: number
}

interface RequestState {
  requestId: number
  serviceId: number
  lon: number | null
  lat: number | null
}

interface FeedItem {
  id: number
  eventType: string
  text: string
  at: string
}

const trucks = reactive(new Map<number, TruckState>())
const requests = reactive<RequestState[]>([])

const serviceStatuses = reactive(new Map<number, ServiceStatus>())
const statusCounts = reactive<Record<ServiceStatus, number>>({
  Possible: 0, Await: 0, Complete: 0, Canceled: 0, CantComplete: 0,
})

// Rullende feed af de seneste events (nyeste først), med et loft saa den
// ikke vokser uendeligt i en langvarig session.
const MAX_FEED = 50
let feedSeq = 0
const feed = reactive<FeedItem[]>([])

function pushFeed (eventType: string, text: string) {
  feed.unshift({ id: feedSeq++, eventType, text, at: new Date().toLocaleTimeString() })
  if (feed.length > MAX_FEED) feed.pop()
}

function upsertTruck (p: RoutePositionPayload) {
  if (p.lon == null || p.lat == null) return
  trucks.set(p.carId, {
    carId: p.carId,
    lon: p.lon,
    lat: p.lat,
    heading: p.heading,
    speed: p.speed,
    routeId: p.routeId,
  })
  pushFeed('RoutePositionUpdate', `Bil ${p.carId} paa rute ${p.routeId}`)
}

function addRequest (p: RequestCreatedPayload) {
  requests.push({
    requestId: p.requestId,
    serviceId: p.serviceId,
    lon: p.lon,
    lat: p.lat,
  })
  pushFeed('RequestCreated', `Ny anmodning #${p.requestId}`)
}

function applyServiceUpdate (p: ServiceUpdatePayload) {
  const previous = serviceStatuses.get(p.serviceId)
  if (previous) statusCounts[previous]--

  serviceStatuses.set(p.serviceId, p.status)
  statusCounts[p.status]++
  pushFeed('ServiceUpdate', `Service ${p.serviceId} -> ${p.status}`)
}

export function useEventStore () {
  return { trucks, requests, feed, statusCounts, upsertTruck, addRequest, applyServiceUpdate }
}