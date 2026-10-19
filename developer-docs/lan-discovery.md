# Native LAN discovery

Native clients search immediately when the Join menu opens, then repeat the search
every five seconds while LAN is selected. UDP requests use port 47777 and retain
the existing application header and response format. The game port comes from the
host's response.

Each search sends to `255.255.255.255` and the subnet broadcast address of each
active IPv4 adapter, calculated using its actual subnet mask. Duplicate targets,
loopback and tunnel adapters, invalid masks, and /31 or /32 networks are excluded.
Interfaces are read again on each search to pick up network changes. Platforms
without adapter information retain the limited-broadcast fallback. A failed send
to one target does not cancel the other targets.

This helps machines with multiple connections, where the operating system may send
the limited broadcast over an adapter that cannot reach the host. It does not
discover arbitrary routed subnets: routers do not forward limited broadcasts,
and directed broadcasts are commonly blocked. See
[RFC 919](https://www.rfc-editor.org/rfc/rfc919) and
[RFC 2644](https://www.rfc-editor.org/rfc/rfc2644).

If a host is missing, try its reachable IPv4 address in the Room IP field. If this
works, game traffic is reachable even though discovery broadcasts may be blocked.
If it fails too, check firewall rules and guest Wi-Fi/client isolation, or use
Internet mode. Browser LAN remains limited to tabs of the same browser and origin;
see [browser LAN](browser-lan.md).

## Validation

Run `LanDiscoveryTests` and `BrowserLanTests` through the Unity CLI PlayMode runner.
The native tests cover subnet calculations, fallback, multiple UDP destinations,
response payloads, rapid stop/restart, and socket cleanup using loopback sockets.

Before release, verify on two physical native devices:

1. Host a game on one device and open Join on the other. Check immediate discovery
   and joining, then reopen Join several times.
2. Enable a second adapter or VPN on the client, with the host reachable through
   the non-default adapter. Check that the host still appears.
3. Change the client's active network with Join open; check the next refresh.
4. Put the devices on routed subnets that block broadcasts but permit game traffic.
   Confirm manual IP joining still works. An empty discovery list is expected.
5. Repeat on supported mobile platforms, with local-network permission granted.

Loopback tests cannot verify physical adapter routing, router policy, mobile
permissions, or firewall behavior.
