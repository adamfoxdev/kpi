/** Lets any page tell the header to re-fetch the alert badge (e.g. after a reading is saved). */
export const ALERTS_CHANGED = 'alerts-changed'
export const notifyAlertsChanged = () => window.dispatchEvent(new Event(ALERTS_CHANGED))
