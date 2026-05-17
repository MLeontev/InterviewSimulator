import { check, sleep } from 'k6';
import http from 'k6/http';

const BASE_URL = 'https://app.siminterview.ru';
const KEYCLOAK_URL = 'https://auth.siminterview.ru';
const REALM = 'InterviewSimulator';
const CLIENT_ID = 'interview-public-client';

export const options = {
  stages: [
    { duration: '30s', target: 50 },
    { duration: '1m', target: 100 },
    { duration: '1m', target: 150 },
    { duration: '30s', target: 0 },
  ],
  thresholds: {
    http_req_duration: ['p(95)<500'],
  },
};

export function setup() {
  const res = http.post(
    `${KEYCLOAK_URL}/realms/${REALM}/protocol/openid-connect/token`,
    {
      grant_type: 'password',
      client_id: CLIENT_ID,
      username: 'load@test.com',
      password: 'loadpassword',
    },
  );

  const token = res.json('access_token');
  console.log('Token obtained:', token ? 'YES' : 'NO');
  return { token };
}

export default function (data) {
  const params = {
    headers: {
      Authorization: `Bearer ${data.token}`,
      'Content-Type': 'application/json',
    },
  };

  const dashboardRes = http.batch([
    ['GET', `${BASE_URL}/api/v1/users/me`, null, params],
    ['GET', `${BASE_URL}/api/v1/interview-sessions`, null, params],
    ['GET', `${BASE_URL}/api/v1/interview-sessions/current`, null, params],
  ]);

  check(dashboardRes[0], {
    'users/me OK': (r) => r.status === 200,
  });

  check(dashboardRes[1], {
    'history OK': (r) => r.status === 200,
  });

  check(dashboardRes[2], {
    'current session 200/404': (r) => r.status === 200 || r.status === 404,
  });

  sleep(2);

  const presetId = '00000005-0000-0000-0000-000000000001';
  const presetsRes = http.batch([
    ['GET', `${BASE_URL}/api/v1/interview-presets`, null, params],
    ['GET', `${BASE_URL}/api/v1/interview-presets/${presetId}`, null, params],
  ]);

  check(presetsRes[0], {
    'presets list OK': (r) => r.status === 200,
  });

  check(presetsRes[1], {
    'preset details OK': (r) => r.status === 200,
  });

  sleep(2);
}
