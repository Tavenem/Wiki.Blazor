import typescript from '@rollup/plugin-typescript';
import { nodeResolve } from '@rollup/plugin-node-resolve';
import { terser } from 'rollup-plugin-terser';

let plugins = [
    typescript(),
    nodeResolve({
        mainFields: ['module', 'main'],
        extensions: ['.mjs', '.js', '.json', '.node', '.ts'],
    }),
];
let jsPlugins = [];
if (process.env.build === 'Release') {
    plugins.push(terser());
    jsPlugins.push(terser());
}

export default [{
    input: "./scripts/tavenem-emoji.ts",
    output: {
        format: 'es',
        sourcemap: true,
    },
    plugins: plugins,
}, {
    input: "./scripts/tavenem-timezone.js",
    output: {
        format: 'es',
        sourcemap: true,
    },
    plugins: jsPlugins,
}];