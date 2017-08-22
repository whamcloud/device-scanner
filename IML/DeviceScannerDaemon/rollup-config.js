import baseConfig from '../../base-rollup-config.js';

export default Object.assign({}, baseConfig, {
  input: 'IML/DeviceScannerDaemon/Server.fs',
  external: ['stream', 'net'],
  output: {
    file: './dist/device-scanner-daemon/device-scanner-daemon',
    format: 'cjs'
  }
});
