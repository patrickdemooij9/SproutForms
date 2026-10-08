<script setup lang="ts">
import type { OptionsFieldConfiguration } from '@sproutforms/client';
import { computed } from 'vue';
import { fieldControlProps } from '../../fieldProps';

const props = defineProps(fieldControlProps);
const emit = defineEmits<{ 'update:modelValue': [value: string] }>();

const options = computed(() => ((props.field.configuration ?? {}) as Partial<OptionsFieldConfiguration>).options ?? []);
</script>

<!-- Renders its own label, as the legend of its options. The fieldset has the field's id, so a submit with an error can focus it -->
<template>
    <fieldset :id="id" class="form-choices" tabindex="-1" :aria-invalid="invalid" :aria-describedby="describedBy">
        <legend>
            {{ field.label }}
            <span v-if="required" class="required">*</span>
        </legend>
        <label v-for="(option, index) in options" :key="option.value" class="form-choice" :for="`${id}_${index}`">
            <input
                :id="`${id}_${index}`"
                type="radio"
                :name="path"
                :value="option.value"
                :checked="modelValue === option.value"
                :disabled="disabled"
                @change="emit('update:modelValue', option.value)"
            />
            <span>{{ option.label }}</span>
        </label>
    </fieldset>
</template>
